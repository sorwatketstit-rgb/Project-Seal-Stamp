using UnityEngine;

namespace SM64
{
    public class PlayerLongJumpState : PlayerState
    {
        private Vector3 _jumpDirection;
        private float _forwardSpeed;

        public PlayerLongJumpState(SM64PlayerController controller, PlayerStateMachine stateMachine) 
            : base(controller, stateMachine) { }

        public override void Enter()
        {
            Controller.ResetAirJumps();
            Controller.VerticalVelocity = Controller.longJumpForce;

            _jumpDirection = Controller.transform.forward;
            _forwardSpeed = Controller.runSpeed * 1.35f;
            Controller.HorizontalVelocity = _jumpDirection * _forwardSpeed;
        }

        public override void HandleInput()
        {
            // Wall jump is possible from a long jump if colliding with a wall
            if (Input != null && Input.JumpPressed && Controller.CheckWall(out Vector3 wallNormal))
            {
                Controller.WallJumpState.SetWallNormal(wallNormal);
                StateMachine.ChangeState(Controller.WallJumpState);
            }
        }

        public override void LogicUpdate()
        {
            if (Controller.VerticalVelocity <= 0f && Controller.IsGrounded())
            {
                StateMachine.ChangeState(Controller.GetLandingMovementState());
            }
        }

        public override void PhysicsUpdate()
        {
            Controller.ApplyGravity();

            // Long jumps have committed momentum with subtle air steering
            Vector3 desiredDir = Controller.GetCameraRelativeInput(Input != null ? Input.MoveInput : Vector2.zero);
            if (desiredDir.sqrMagnitude > 0.1f)
            {
                _jumpDirection = Vector3.RotateTowards(_jumpDirection, desiredDir, Controller.airControl * 0.5f * Time.deltaTime, 0f);
            }

            // Slight forward friction in the air
            _forwardSpeed = Mathf.Max(_forwardSpeed - Time.deltaTime * 0.5f, Controller.runSpeed);
            Controller.HorizontalVelocity = _jumpDirection * _forwardSpeed;
        }
    }
}
