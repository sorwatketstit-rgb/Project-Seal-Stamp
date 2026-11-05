using UnityEngine;

namespace SM64
{
    /// <summary>
    /// Seal Swim State — a sub-state rooted in <see cref="PlayerSealState"/>.
    /// Activated exclusively while in Seal state when contacting the water collision layer.
    /// In this state:
    /// - The player floats above the water collider.
    /// - Moves freely at high speed without periodic nudge intervals.
    /// - Spawns wake / swimming particles behind the character.
    /// - When reverting to human (Space/Jump), flings upward in looking direction.
    /// - Automatically transitions back to land seal when climbing onto solid dry ground.
    /// </summary>
    public class PlayerSealSwimState : PlayerSealState
    {
        private readonly PlayerSealState _root;
        private bool _returningToSeal;
        private float _swimParticleTimer;

        public PlayerSealSwimState(SM64PlayerController controller, PlayerStateMachine stateMachine, PlayerSealState root)
            : base(controller, stateMachine, root)
        {
            _root = root;
        }

        public override void Enter()
        {
            _returningToSeal = false;
            _swimParticleTimer = 0f;

            // Player is already a seal, so do not play transform smoke or swap model
            Controller.VerticalVelocity = 0f;
        }

        public override void HandleInput()
        {
            if (Input == null) return;

            // Reverting from seal to human while swimming flings upward in the direction player is looking at
            if (Input.JumpPressed)
            {
                RevertWithFling();
                return;
            }

            // Spit item if holding one and Shift pressed
            if (Input.KickPressed && Seal != null && Seal.CanSpit)
            {
                Seal.Spit();
            }
        }

        public override void LogicUpdate()
        {
            // If we are no longer in water, return to land seal
            if (!Controller.IsInWater)
            {
                ReturnToLandSeal();
            }
        }

        public override void PhysicsUpdate()
        {
            // 1. Water floating: find the water surface and adjust vertical velocity
            float waterSurfaceY = Controller.GetWaterSurfaceY();
            float targetY = waterSurfaceY + Controller.swimFloatHeightOffset;
            float currentY = Controller.transform.position.y;
            float diffY = targetY - currentY;

            Controller.VerticalVelocity = diffY * Controller.swimBuoyancySpring;

            // 2. Free, fast swimming movement (no periodic nudge interval)
            Vector2 moveInput = Input != null ? Input.MoveInput : Vector2.zero;
            Vector3 desiredDir = Controller.GetCameraRelativeInput(moveInput);

            if (desiredDir.sqrMagnitude > 0.001f)
            {
                Controller.RotateTowards(desiredDir, Controller.turnSmoothTime);
                Controller.HorizontalVelocity = Vector3.MoveTowards(
                    Controller.HorizontalVelocity,
                    desiredDir * Controller.swimSpeed,
                    Controller.swimAcceleration * Time.deltaTime
                );

                // 3. Particle spawn behind the seal while swimming
                _swimParticleTimer -= Time.deltaTime;
                if (_swimParticleTimer <= 0f)
                {
                    _swimParticleTimer = Controller.SwimParticleInterval;
                    Controller.SpawnSwimParticle();
                }
            }
            else
            {
                Controller.HorizontalVelocity = Vector3.MoveTowards(
                    Controller.HorizontalVelocity,
                    Vector3.zero,
                    Controller.swimDeceleration * Time.deltaTime
                );
            }
        }

        public override void Exit()
        {
            if (_returningToSeal)
            {
                _returningToSeal = false;
                return; // still a seal on land; skip revert VFX and model reset
            }

            // Fully leaving seal mode (e.g. breached / flung to human or respawned)
            LeaveSealMode();
        }

        private void ReturnToLandSeal()
        {
            if (_returningToSeal) return;
            _returningToSeal = true;
            StateMachine.ChangeState(_root);
        }

        private void RevertWithFling()
        {
            // Upward and forward fling in the direction the player is looking at
            Vector3 lookDir = Controller.transform.forward;

            Controller.VerticalVelocity = Controller.swimFlingUpwardForce;
            Controller.HorizontalVelocity = lookDir * Controller.swimFlingForwardForce;

            // Lift slightly to breach water boundary cleanly
            Controller.CharacterController.Move(Vector3.up * 0.45f);

            // Grant a short grace period (0.2s) so the upward impulse can initiate
            // If the player remains inside water or lands back in water, they will die!
            Controller.GrantWaterGracePeriod(0.2f);

            StateMachine.ChangeState(Controller.AirborneState);
        }
    }
}

