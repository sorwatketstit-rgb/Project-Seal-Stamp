using UnityEngine;

namespace SM64
{
    /// <summary>
    /// Grounded movement state for Running.
    /// Moves at high speed with full strafing steering.
    /// When movement keys are released, maintains velocity for runStopDelay (0.25s) then switches to WalkingState
    /// without changing the player's speed.
    /// </summary>
    public class PlayerRunningState : PlayerGroundedState
    {
        private float _particleTimer;
        private float _stopMoveTimer;

        public PlayerRunningState(SM64PlayerController controller, PlayerStateMachine stateMachine) 
            : base(controller, stateMachine) { }

        public override void Enter()
        {
            base.Enter();
            Controller.CurrentMovementSubState = MovementSubState.Running;
            _particleTimer = 0f;
            _stopMoveTimer = 0f;

            // Spawn first running particle upon entering the state
            Controller.SpawnRunningParticle();
        }

        public override void HandleInput()
        {
            base.HandleInput();

            // Kick while running — placeholder for future implementation
            if (Input != null && Input.KickPressed)
            {
                Debug.Log("[BallKick] Kick pressed while RUNNING — running kick not yet implemented, will be added in future.");
            }
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            // Spawn running particle periodically every runParticleInterval (0.5s)
            _particleTimer += Time.deltaTime;
            if (_particleTimer >= Controller.RunParticleInterval)
            {
                _particleTimer = 0f;
                Controller.SpawnRunningParticle();
            }

            // If player stopped pressing movement keys while grounded,
            // wait for runStopDelay (0.25s) then immediately switch to grounded walking state without changing speed
            bool isPressingMovement = Input != null && Input.MoveInput.sqrMagnitude > 0.01f;
            if (isPressingMovement)
            {
                _stopMoveTimer = 0f;
            }
            else
            {
                _stopMoveTimer += Time.deltaTime;
                if (_stopMoveTimer >= Controller.runStopDelay)
                {
                    if (Controller.IsGrounded())
                    {
                        StateMachine.ChangeState(Controller.WalkingState);
                        return;
                    }
                }
            }
        }

        public override void PhysicsUpdate()
        {
            Vector3 desiredDir = Controller.GetCameraRelativeInput(Input != null ? Input.MoveInput : Vector2.zero);
            Vector3 currentHoriz = Controller.HorizontalVelocity;
            float currentSpeed = currentHoriz.magnitude;

            if (desiredDir.magnitude > 0.1f)
            {
                // Full running speed with free strafe steering
                float targetSpeed = Controller.runSpeed;
                currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, Controller.acceleration * 1.5f * Time.deltaTime);

                Controller.RotateTowards(desiredDir, Controller.turnSmoothTime);
                Controller.HorizontalVelocity = Controller.transform.forward * currentSpeed;
            }
            else
            {
                // When movement keys are stopped, do not change the player's speed
                Controller.HorizontalVelocity = currentHoriz;
            }
        }

        public override void Exit()
        {
            base.Exit();
            _particleTimer = 0f;
            _stopMoveTimer = 0f;
        }
    }
}
