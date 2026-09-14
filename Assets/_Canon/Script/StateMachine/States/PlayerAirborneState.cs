using UnityEngine;

namespace SM64
{
    public class PlayerAirborneState : PlayerState
    {
        private float _coyoteTimeCounter;
        private bool _canCoyoteJump;

        public PlayerAirborneState(SM64PlayerController controller, PlayerStateMachine stateMachine) 
            : base(controller, stateMachine) { }

        public void SetupJump(bool consumedAirJump)
        {
            _canCoyoteJump = false;
            _coyoteTimeCounter = 0f;
            if (consumedAirJump)
            {
                Controller.AirJumpsRemaining--;
            }
        }

        public void SetupFallWithCoyoteTime()
        {
            _canCoyoteJump = true;
            _coyoteTimeCounter = Controller.coyoteTime;
        }

        public override void Enter()
        {
        }

        public override void HandleInput()
        {
            if (Input == null) return;

            // Ground Pound trigger
            if (Input.CrouchPressed)
            {
                StateMachine.ChangeState(Controller.GroundPoundState);
                return;
            }

            // Jump handling
            if (Input.JumpPressed)
            {
                // Wall jump priority if near a wall
                if (Controller.CheckWall(out Vector3 wallNormal))
                {
                    Controller.WallJumpState.SetWallNormal(wallNormal);
                    StateMachine.ChangeState(Controller.WallJumpState);
                    return;
                }

                // Coyote time ground jump (stepped off a ledge)
                if (_canCoyoteJump && _coyoteTimeCounter > 0f)
                {
                    _canCoyoteJump = false;
                    _coyoteTimeCounter = 0f;
                    Controller.VerticalVelocity = Controller.jumpForce;
                    return;
                }

                // Optimized Mid-Air Double Jump
                if (Controller.AirJumpsRemaining > 0)
                {
                    ExecuteDoubleJump();
                    return;
                }

                // Buffer the jump for immediate execution upon landing
                Controller.BufferJump();
            }
        }

        private void ExecuteDoubleJump()
        {
            Controller.AirJumpsRemaining--;

            // Instant vertical reset provides snappy and predictable double jump height
            Controller.VerticalVelocity = Controller.doubleJumpForce;

            // Apply responsive air redirection if directional input is provided
            Vector3 desiredDir = Controller.GetCameraRelativeInput(Input.MoveInput);
            if (desiredDir.sqrMagnitude > 0.05f)
            {
                Controller.RotateTowards(desiredDir, Controller.turnSmoothTime);
                float forwardSpeed = Mathf.Max(Controller.HorizontalVelocity.magnitude, Controller.walkSpeed * 1.2f);
                Controller.HorizontalVelocity = Controller.transform.forward * forwardSpeed;
            }
        }

        public override void LogicUpdate()
        {
            // Cooldown for ledge grabbing
            if (Controller.LedgeGrabState != null && Controller.LedgeGrabState.LedgeCooldownTimer > 0f)
            {
                Controller.LedgeGrabState.LedgeCooldownTimer -= Time.deltaTime;
            }

            // Coyote timer countdown
            if (_canCoyoteJump && _coyoteTimeCounter > 0f)
            {
                _coyoteTimeCounter -= Time.deltaTime;
                if (_coyoteTimeCounter <= 0f)
                {
                    _canCoyoteJump = false;
                }
            }

            // 2-Dot Sensor Evaluation (Wall Slide vs Ledge Grab)
            if (Controller.LedgeGrabState == null || Controller.LedgeGrabState.LedgeCooldownTimer <= 0f)
            {
                bool hasWallContact = Controller.CheckWallSensors(out bool middleHit, out bool topHit, out RaycastHit middleHitInfo, out RaycastHit topHitInfo);

                if (hasWallContact)
                {
                    // Case 1: Both dots hit -> Wall Slide (Cling + Slow Slide)
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
                    // Case 2: Only Middle dot hits (Top dot is clear) -> Ledge Grab
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

            // Landing check: Evaluates speed threshold (walkSpeed * 1.5) to decide Walking vs Running
            if (Controller.VerticalVelocity <= 0f && Controller.IsGrounded())
            {
                StateMachine.ChangeState(Controller.GetLandingMovementState());
            }
        }

        public override void PhysicsUpdate()
        {
            // Apply gravity
            Controller.ApplyGravity();

            // Gradually decay horizontal air speed (air resistance)
            Vector3 currentHoriz = Controller.HorizontalVelocity;
            currentHoriz = Vector3.MoveTowards(currentHoriz, Vector3.zero, Controller.airSpeedDecay * Time.deltaTime);

            // Air control steering
            Vector3 desiredDir = Controller.GetCameraRelativeInput(Input != null ? Input.MoveInput : Vector2.zero);
            if (desiredDir.sqrMagnitude > 0.01f)
            {
                Controller.RotateTowards(desiredDir, Controller.turnSmoothTime * 1.5f);
                float speed = currentHoriz.magnitude;
                Vector3 targetHoriz = Controller.transform.forward * speed;
                Controller.HorizontalVelocity = Vector3.Lerp(currentHoriz, targetHoriz, Controller.airControl * Time.deltaTime * 5f);
            }
            else
            {
                Controller.HorizontalVelocity = currentHoriz;
            }
        }

        public override void Exit()
        {
            _canCoyoteJump = false;
            _coyoteTimeCounter = 0f;
        }
    }
}
