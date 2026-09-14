using UnityEngine;

namespace SM64
{
    public class PlayerBackflipState : PlayerState
    {
        private Vector3 _backwardDir;
        private float _backwardSpeed;

        public PlayerBackflipState(SM64PlayerController controller, PlayerStateMachine stateMachine) 
            : base(controller, stateMachine) { }

        public override void Enter()
        {
            Controller.ResetAirJumps();
            Controller.VerticalVelocity = Controller.backflipForce;

            _backwardDir = -Controller.transform.forward;
            _backwardSpeed = Controller.runSpeed * 0.7f;
            Controller.HorizontalVelocity = _backwardDir * _backwardSpeed;
        }

        public override void HandleInput()
        {
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

            // Air control allows slight adjustments
            Vector3 desiredDir = Controller.GetCameraRelativeInput(Input != null ? Input.MoveInput : Vector2.zero);
            if (desiredDir.sqrMagnitude > 0.1f)
            {
                _backwardDir = Vector3.RotateTowards(_backwardDir, desiredDir, Controller.airControl * Time.deltaTime, 0f);
            }

            _backwardSpeed = Mathf.MoveTowards(_backwardSpeed, 0f, Time.deltaTime * 2f);
            Controller.HorizontalVelocity = _backwardDir * _backwardSpeed;
        }
    }
}
