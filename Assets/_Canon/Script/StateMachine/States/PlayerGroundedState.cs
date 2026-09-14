using UnityEngine;

namespace SM64
{
    public class PlayerGroundedState : PlayerState
    {
        private bool _isLongJumpPrep;
        private bool _isBackflipPrep;
        private float _prepTimer;
        private const float PrepTimeout = 0.5f;

        public PlayerGroundedState(SM64PlayerController controller, PlayerStateMachine stateMachine) 
            : base(controller, stateMachine) { }

        public override void Enter()
        {
            Controller.ResetAirJumps();
            Controller.VerticalVelocity = -2f; // keeps controller grounded
            _isLongJumpPrep = false;
            _isBackflipPrep = false;
            _prepTimer = 0f;

            // Check if jump buffer was active upon landing
            if (Controller.ConsumeBufferedJump())
            {
                TriggerJump();
            }
        }

        public override void HandleInput()
        {
            if (Input == null) return;

            // Crouch prep handling for Long Jump / Backflip
            if (Input.CrouchPressed)
            {
                Vector3 moveDir = Controller.GetCameraRelativeInput(Input.MoveInput);
                if (moveDir.sqrMagnitude > 0.05f)
                {
                    _isLongJumpPrep = true;
                    _isBackflipPrep = false;
                }
                else
                {
                    _isBackflipPrep = true;
                    _isLongJumpPrep = false;
                }
                _prepTimer = PrepTimeout;
            }

            if (_isLongJumpPrep || _isBackflipPrep)
            {
                _prepTimer -= Time.deltaTime;
                if (_prepTimer <= 0f || !Input.CrouchHeld)
                {
                    _isLongJumpPrep = false;
                    _isBackflipPrep = false;
                }
            }

            // Jump trigger
            if (Input.JumpPressed)
            {
                TriggerJump();
            }
        }

        private void TriggerJump()
        {
            if (_isLongJumpPrep)
            {
                StateMachine.ChangeState(Controller.LongJumpState);
                return;
            }

            if (_isBackflipPrep)
            {
                StateMachine.ChangeState(Controller.BackflipState);
                return;
            }

            // Standard ground jump
            Controller.VerticalVelocity = Controller.jumpForce;
            Controller.AirborneState.SetupJump(consumedAirJump: false);
            StateMachine.ChangeState(Controller.AirborneState);
        }

        public override void LogicUpdate()
        {
            // If walked off a ledge into the air
            if (!Controller.IsGrounded())
            {
                Controller.AirborneState.SetupFallWithCoyoteTime();
                StateMachine.ChangeState(Controller.AirborneState);
            }
        }

        public override void PhysicsUpdate()
        {
            Vector3 desiredDir = Controller.GetCameraRelativeInput(Input != null ? Input.MoveInput : Vector2.zero);
            float targetSpeed = desiredDir.magnitude > 0.1f
                ? ((Input != null && Input.CrouchHeld) ? Controller.walkSpeed : Controller.runSpeed)
                : 0f;

            // Smooth horizontal speed
            Vector3 currentHoriz = Controller.HorizontalVelocity;
            float currentSpeed = currentHoriz.magnitude;
            float accel = (targetSpeed > currentSpeed) ? Controller.acceleration : Controller.deceleration;
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accel * Time.deltaTime);

            // Smooth rotation towards movement direction
            if (desiredDir.sqrMagnitude > 0.001f)
            {
                Controller.RotateTowards(desiredDir, Controller.turnSmoothTime);
            }

            Vector3 newHoriz = (currentSpeed > 0.001f) ? Controller.transform.forward * currentSpeed : Vector3.zero;
            Controller.HorizontalVelocity = newHoriz;
        }

        public override void Exit()
        {
            _isLongJumpPrep = false;
            _isBackflipPrep = false;
        }
    }
}
