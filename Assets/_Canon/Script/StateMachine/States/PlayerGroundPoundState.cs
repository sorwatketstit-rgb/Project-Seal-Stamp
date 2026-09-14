using UnityEngine;

namespace SM64
{
    public class PlayerGroundPoundState : PlayerState
    {
        private enum Phase { Stall, Slam, Recovery }
        private Phase _phase;
        private float _phaseTimer;

        private const float StallDuration = 0.2f;
        private const float RecoveryDuration = 0.25f;
        private const float SlamSpeed = -25f;

        public PlayerGroundPoundState(SM64PlayerController controller, PlayerStateMachine stateMachine) 
            : base(controller, stateMachine) { }

        public override void Enter()
        {
            _phase = Phase.Stall;
            _phaseTimer = StallDuration;

            // Zero out velocities during stall
            Controller.HorizontalVelocity = Vector3.zero;
            Controller.VerticalVelocity = 0f;
        }

        public override void HandleInput()
        {
            // During recovery, allow instant jump cancel
            if (_phase == Phase.Recovery && Input != null && Input.JumpPressed)
            {
                Controller.VerticalVelocity = Controller.jumpForce;
                Controller.AirborneState.SetupJump(consumedAirJump: false);
                StateMachine.ChangeState(Controller.AirborneState);
            }
        }

        public override void LogicUpdate()
        {
            _phaseTimer -= Time.deltaTime;

            switch (_phase)
            {
                case Phase.Stall:
                    if (_phaseTimer <= 0f)
                    {
                        _phase = Phase.Slam;
                        Controller.VerticalVelocity = SlamSpeed;
                    }
                    break;

                case Phase.Slam:
                    if (Controller.IsGrounded())
                    {
                        _phase = Phase.Recovery;
                        _phaseTimer = RecoveryDuration;
                        Controller.VerticalVelocity = -2f;
                        Controller.HorizontalVelocity = Vector3.zero;
                    }
                    break;

                case Phase.Recovery:
                    if (_phaseTimer <= 0f)
                    {
                        Controller.CurrentMovementSubState = MovementSubState.Walking;
                        StateMachine.ChangeState(Controller.WalkingState);
                    }
                    break;
            }
        }

        public override void PhysicsUpdate()
        {
            if (_phase == Phase.Slam)
            {
                // Rapid descent
                Controller.VerticalVelocity = SlamSpeed;
            }
        }
    }
}
