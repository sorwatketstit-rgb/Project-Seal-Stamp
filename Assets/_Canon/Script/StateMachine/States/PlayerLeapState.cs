using UnityEngine;

namespace SM64
{
    /// <summary>
    /// Leap State — triggered after a successful double jump when the player presses Jump again.
    /// Launches the player forward in the direction they are currently facing with a strong
    /// horizontal impulse and moderate upward boost, consuming all remaining air jumps.
    /// </summary>
    public class PlayerLeapState : PlayerState
    {
        private Vector3 _leapDirection;
        private float   _leapSpeed;

        public PlayerLeapState(SM64PlayerController controller, PlayerStateMachine stateMachine)
            : base(controller, stateMachine) { }

        public override void Enter()
        {
            // No air jumps left after leaping
            Controller.AirJumpsRemaining = 0;

            // Vertical boost — slightly lower than a full jump so it reads as a forward surge
            Controller.VerticalVelocity = Controller.leapForce;

            // Commit to the direction the character is currently facing
            _leapDirection = Controller.transform.forward;
            _leapSpeed     = Controller.runSpeed * Controller.leapSpeedMultiplier;

            Controller.HorizontalVelocity = _leapDirection * _leapSpeed;
        }

        public override void HandleInput()
        {
            if (Input == null) return;

            // Allow wall jump escape from a Leap if the player crashes into a wall
            if (Input.JumpPressed && Controller.CheckWall(out Vector3 wallNormal))
            {
                Controller.WallJumpState.SetWallNormal(wallNormal);
                StateMachine.ChangeState(Controller.WallJumpState);
            }
        }

        public override void LogicUpdate()
        {
            // 2-Dot Sensor evaluation (same as Airborne — allow Wall Slide / Ledge Grab mid-leap)
            if (Controller.LedgeGrabState == null || Controller.LedgeGrabState.LedgeCooldownTimer <= 0f)
            {
                bool hasWallContact = Controller.CheckWallSensors(
                    out bool middleHit, out bool topHit,
                    out RaycastHit middleHitInfo, out RaycastHit topHitInfo);

                if (hasWallContact)
                {
                    if (middleHit && topHit)
                    {
                        float facingDot = Vector3.Dot(Controller.transform.forward, -middleHitInfo.normal);
                        if (facingDot > 0.15f || Controller.VerticalVelocity < 0f)
                        {
                            Controller.WallSlideState.SetWallContact(middleHitInfo.normal);
                            StateMachine.ChangeState(Controller.WallSlideState);
                            return;
                        }
                    }
                    else if (middleHit && !topHit)
                    {
                        if (Controller.VerticalVelocity < 5f)
                        {
                            if (Controller.FindLedgePositions(middleHitInfo, out Vector3 hangPos, out Vector3 climbPos, out Vector3 wallNormal))
                            {
                                Controller.LedgeGrabState.SetupLedge(hangPos, climbPos, wallNormal);
                                StateMachine.ChangeState(Controller.LedgeGrabState);
                                return;
                            }
                        }
                    }
                }
            }

            // Landing — carry the leap momentum into the running state if speed qualifies
            if (Controller.VerticalVelocity <= 0f && Controller.IsGrounded())
            {
                StateMachine.ChangeState(Controller.GetLandingMovementState());
            }
        }

        public override void PhysicsUpdate()
        {
            Controller.ApplyGravity();

            // Leap momentum decays gently — faster than normal air resistance for a committed feel
            _leapSpeed = Mathf.MoveTowards(_leapSpeed, Controller.runSpeed * 0.8f,
                Controller.airSpeedDecay * 1.5f * Time.deltaTime);

            // Very limited air steering: the player is committed to the leap arc
            Vector3 desiredDir = Controller.GetCameraRelativeInput(Input != null ? Input.MoveInput : Vector2.zero);
            if (desiredDir.sqrMagnitude > 0.1f)
            {
                _leapDirection = Vector3.RotateTowards(
                    _leapDirection, desiredDir,
                    Controller.airControl * 0.4f * Time.deltaTime, 0f);
            }

            Controller.HorizontalVelocity = _leapDirection * _leapSpeed;
        }

        public override void Exit() { }
    }
}
