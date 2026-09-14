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
            // Enter air state
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
                // Wall jump priority
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

                // If no jumps remain, buffer the jump for landing
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
                float forwardSpeed = Mathf.Max(Controller.HorizontalVelocity.magnitude, Controller.runSpeed * 0.8f);
                Controller.HorizontalVelocity = Controller.transform.forward * forwardSpeed;
            }
        }

        public override void LogicUpdate()
        {
            // Coyote timer countdown
            if (_canCoyoteJump && _coyoteTimeCounter > 0f)
            {
                _coyoteTimeCounter -= Time.deltaTime;
                if (_coyoteTimeCounter <= 0f)
                {
                    _canCoyoteJump = false;
                }
            }

            // Landing check
            if (Controller.VerticalVelocity <= 0f && Controller.IsGrounded())
            {
                StateMachine.ChangeState(Controller.GroundedState);
            }
        }

        public override void PhysicsUpdate()
        {
            // Apply gravity
            Controller.ApplyGravity();

            // Air control for horizontal steering
            Vector3 desiredDir = Controller.GetCameraRelativeInput(Input != null ? Input.MoveInput : Vector2.zero);
            Vector3 currentHoriz = Controller.HorizontalVelocity;

            if (desiredDir.sqrMagnitude > 0.01f)
            {
                Controller.RotateTowards(desiredDir, Controller.turnSmoothTime * 1.5f);
                Vector3 targetHoriz = Controller.transform.forward * Controller.runSpeed;
                Controller.HorizontalVelocity = Vector3.Lerp(currentHoriz, targetHoriz, Controller.airControl * Time.deltaTime * 5f);
            }
            else
            {
                // Slight air drag
                Controller.HorizontalVelocity = Vector3.MoveTowards(currentHoriz, Vector3.zero, Controller.deceleration * 0.2f * Time.deltaTime);
            }
        }

        public override void Exit()
        {
            _canCoyoteJump = false;
            _coyoteTimeCounter = 0f;
        }
    }
}
