using UnityEngine;

namespace SM64
{
    public class PlayerWallJumpState : PlayerState
    {
        private Vector3 _wallNormal;
        private float _impulseTimer;
        private const float ImpulseDuration = 0.22f;

        public PlayerWallJumpState(SM64PlayerController controller, PlayerStateMachine stateMachine) 
            : base(controller, stateMachine) { }

        public void SetWallNormal(Vector3 normal)
        {
            _wallNormal = normal;
        }

        public override void Enter()
        {
            Controller.ResetAirJumps();
            Controller.VerticalVelocity = Controller.wallJumpForce;

            Vector3 pushDirection = (_wallNormal + Vector3.up * 0.3f).normalized;
            Vector3 horizDirection = new Vector3(pushDirection.x, 0, pushDirection.z).normalized;

            Controller.RotateTowards(horizDirection, 0.01f);
            Controller.HorizontalVelocity = horizDirection * (Controller.wallJumpForce * 0.85f);

            _impulseTimer = ImpulseDuration;
        }

        public override void LogicUpdate()
        {
            _impulseTimer -= Time.deltaTime;
            if (_impulseTimer <= 0f)
            {
                // Return to general airborne state with air control restored
                StateMachine.ChangeState(Controller.AirborneState);
                return;
            }

            if (Controller.VerticalVelocity <= 0f && Controller.IsGrounded())
            {
                StateMachine.ChangeState(Controller.GroundedState);
            }
        }

        public override void PhysicsUpdate()
        {
            Controller.ApplyGravity();
        }
    }
}
