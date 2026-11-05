using UnityEngine;

namespace SM64
{
    /// <summary>
    /// Seal State — temporarily transforms the player into a seal model with visual effects.
    /// Triggered exclusively:
    /// 1. After leaping, if the player does not touch the ground within 1 second.
    /// 2. When pressing Jump while crouching (Crouch + Space bar).
    ///
    /// Movement: the seal does not walk. While a direction is held it nudges forward once every
    /// <see cref="SealStateMachine.NudgeInterval"/> seconds (settings live on SealStateMachine).
    /// Actions: holding Shift enters <see cref="PlayerSealSuckingState"/> (only possible from here);
    /// pressing Shift while holding an item spits it. Pressing Space (Jump) reverts to normal.
    /// </summary>
    public class PlayerSealState : PlayerState
    {
        protected readonly SealStateMachine Seal;
        private readonly BallKicking _ballKicking;
        private readonly PlayerSealSuckingState _suckingState;
        private readonly PlayerSealSwimState _swimState;

        private bool _requireRelease;   // Shift must be released before it can act again
        private bool _enteringSucking;  // true while handing over to the sucking sub-state
        private bool _enteringSwim;     // true while handing over to the swimming sub-state

        private float _nudgeCooldown;   // time until the next nudge may start
        private float _nudgeTimeLeft;   // time left in the nudge that is currently playing
        private Vector3 _nudgeDir;

        public PlayerSealSwimState SwimState => _swimState;

        public PlayerSealState(SM64PlayerController controller, PlayerStateMachine stateMachine)
            : base(controller, stateMachine)
        {
            Seal = controller.GetComponent<SealStateMachine>();
            _ballKicking = controller.GetComponent<BallKicking>();
            _suckingState = new PlayerSealSuckingState(controller, stateMachine, this);
            _swimState = new PlayerSealSwimState(controller, stateMachine, this);
        }

        /// <summary>Used by the sucking and swimming sub-states so they share the root's references.</summary>
        protected PlayerSealState(SM64PlayerController controller, PlayerStateMachine stateMachine, PlayerSealState root)
            : base(controller, stateMachine)
        {
            Seal = root.Seal;
            _ballKicking = root._ballKicking;
            _suckingState = null;
            _swimState = null;
        }

        public override void Enter()
        {
            // Shift may still be down (from the transform, or from sucking): require a fresh press.
            _requireRelease = true;

            // Coming back from the sucking or swimming sub-states: we are already a seal, so skip the transform.
            if (StateMachine.PreviousState is PlayerSealSuckingState || StateMachine.PreviousState is PlayerSealSwimState)
                return;

            // [TEMPORARY] Swap to seal model
            Controller.SetVisualModel(PlayerVisualModel.Seal);

            // Play transformation particle effect
            Controller.PlaySealTransformParticle();

            if (Controller.IsGrounded())
            {
                Controller.VerticalVelocity = -2f;
            }

            _nudgeCooldown = 0f;
            _nudgeTimeLeft = 0f;

            if (_ballKicking != null) _ballKicking.enabled = false; // Shift is the seal key now
        }

        public override void HandleInput()
        {
            if (Input == null) return;

            // In seal state, pressing Space (Jump) reverts the player back to normal
            if (Input.JumpPressed)
            {
                RevertToNormal();
                return;
            }

            if (!Input.KickHeld) _requireRelease = false;
            if (_requireRelease || Seal == null) return;

            // Holding an item: a fresh Shift press spits it out
            if (Input.KickPressed && Seal.CanSpit)
            {
                Seal.Spit();
                _requireRelease = true;
                return;
            }

            // Holding Shift with empty hands: start sucking (sub-state of seal)
            if (Input.KickHeld && Seal.CanStartSucking)
            {
                _enteringSucking = true;
                StateMachine.ChangeState(_suckingState);
            }
        }

        public override void LogicUpdate()
        {
            // Water check: if seal is in water, transition to swimming sub-state
            if (Controller.IsInWater)
            {
                EnterSwim();
                return;
            }

            // Landing check if entered mid-air (from leap timeout)
            if (Controller.IsGrounded() && Controller.VerticalVelocity <= 0f)
            {
                Controller.VerticalVelocity = -2f;
                Controller.AirborneState.ResetJumpCount();
            }
        }

        public void EnterSwim()
        {
            if (_swimState == null) return;
            _enteringSwim = true;
            StateMachine.ChangeState(_swimState);
        }

        public override void PhysicsUpdate()
        {
            // Apply gravity regardless of whether grounded or mid-air
            Controller.ApplyGravity();

            Vector3 desiredDir = Controller.GetCameraRelativeInput(Input != null ? Input.MoveInput : Vector2.zero);

            if (desiredDir.sqrMagnitude > 0.001f)
            {
                Controller.RotateTowards(desiredDir, Controller.turnSmoothTime);
            }

            UpdateNudge(desiredDir);
        }

        /// <summary>
        /// One short forward burst every NudgeInterval seconds while a direction is held.
        /// The burst starts at NudgeSpeed and fades to zero over NudgeDuration.
        /// </summary>
        private void UpdateNudge(Vector3 desiredDir)
        {
            float dt = Time.deltaTime;

            if (_nudgeCooldown > 0f)
                _nudgeCooldown -= dt;

            float duration = Seal != null ? Seal.NudgeDuration : 0.4f;
            float speed = Seal != null ? Seal.NudgeSpeed : 6f;
            float interval = Seal != null ? Seal.NudgeInterval : 1f;

            bool canStart = _nudgeTimeLeft <= 0f
                            && _nudgeCooldown <= 0f
                            && desiredDir.sqrMagnitude > 0.001f
                            && Controller.IsGrounded();

            if (canStart && duration > 0f)
            {
                _nudgeDir = desiredDir;
                _nudgeTimeLeft = duration;
                _nudgeCooldown = interval;
            }

            if (_nudgeTimeLeft > 0f && duration > 0f)
            {
                _nudgeTimeLeft -= dt;
                float t = Mathf.Clamp01(_nudgeTimeLeft / duration); // 1 -> 0
                Controller.HorizontalVelocity = _nudgeDir * (speed * t);
            }
            else
            {
                // Between nudges the seal just slides to a stop
                Controller.HorizontalVelocity = Vector3.MoveTowards(
                    Controller.HorizontalVelocity, Vector3.zero, Controller.deceleration * dt);
            }
        }

        public override void Exit()
        {
            // Handing over to sub-states (sucking or swimming): still a seal, so keep the model and kick lock.
            if (_enteringSucking || _enteringSwim)
            {
                _enteringSucking = false;
                _enteringSwim = false;
                return;
            }

            LeaveSealMode();
        }

        /// <summary>Everything that must happen when the player really stops being a seal.</summary>
        protected void LeaveSealMode()
        {
            if (Seal != null) Seal.EndSucking();
            if (_ballKicking != null) _ballKicking.enabled = true;

            // [TEMPORARY] Revert model back to default
            Controller.SetVisualModel(PlayerVisualModel.Default);

            // Play particle effect when reverting back from seal state
            Controller.PlaySealRevertParticle();
        }

        protected virtual void RevertToNormal()
        {
            // After the player changes into human state from seal state, they are flung upward in the direction the player is looking at
            Vector3 forwardDir = Controller.transform.forward;
            Controller.VerticalVelocity = Controller.sealRevertFlingUpForce;
            Controller.HorizontalVelocity = forwardDir * Controller.sealRevertFlingForwardForce;
            Controller.CharacterController.Move(Vector3.up * 0.1f);

            // Grant a short grace period (0.2s) so the upward impulse can initiate
            // If the player remains inside water or lands back in water, they will die!
            Controller.GrantWaterGracePeriod(0.2f);

            StateMachine.ChangeState(Controller.AirborneState);
        }
    }
}