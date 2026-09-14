using UnityEngine;

namespace SM64
{
    /// <summary>
    /// Grounded movement state for Walking and Idle.
    /// Tracks sustained movement in one direction; holding for runActivationTime (3s) transitions to RunningState.
    /// </summary>
    public class PlayerWalkingState : PlayerGroundedState
    {
        private float _holdTimer;
        private Vector2 _lastHoldDirection;

        public PlayerWalkingState(SM64PlayerController controller, PlayerStateMachine stateMachine) 
            : base(controller, stateMachine) { }

        public override void Enter()
        {
            base.Enter();
            Controller.CurrentMovementSubState = MovementSubState.Walking;
            _holdTimer = 0f;
            _lastHoldDirection = Vector2.zero;
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            if (Input == null) return;

            // Track continuous hold in 1 direction
            if (Input.MoveInput.magnitude > 0.5f)
            {
                Vector2 currentDir = Input.MoveInput.normalized;

                if (_lastHoldDirection == Vector2.zero || Vector2.Dot(currentDir, _lastHoldDirection) > 0.65f)
                {
                    if (_lastHoldDirection == Vector2.zero)
                    {
                        _lastHoldDirection = currentDir;
                    }

                    _holdTimer += Time.deltaTime;

                    if (_holdTimer >= Controller.runActivationTime)
                    {
                        StateMachine.ChangeState(Controller.RunningState);
                        return;
                    }
                }
                else
                {
                    // Direction changed too abruptly; reset timer
                    _holdTimer = 0f;
                    _lastHoldDirection = currentDir;
                }
            }
            else
            {
                _holdTimer = 0f;
                _lastHoldDirection = Vector2.zero;
            }
        }

        public override void PhysicsUpdate()
        {
            Vector3 desiredDir = Controller.GetCameraRelativeInput(Input != null ? Input.MoveInput : Vector2.zero);
            float targetSpeed = desiredDir.magnitude > 0.1f ? Controller.walkSpeed : 0f;

            // Smooth horizontal walk speed
            Vector3 currentHoriz = Controller.HorizontalVelocity;
            float currentSpeed = currentHoriz.magnitude;
            float accel = (targetSpeed > currentSpeed) ? Controller.acceleration : Controller.deceleration;
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accel * Time.deltaTime);

            // Smooth rotation towards walking direction
            if (desiredDir.sqrMagnitude > 0.001f)
            {
                Controller.RotateTowards(desiredDir, Controller.turnSmoothTime);
            }

            Vector3 newHoriz = (currentSpeed > 0.001f) ? Controller.transform.forward * currentSpeed : Vector3.zero;
            Controller.HorizontalVelocity = newHoriz;
        }

        public override void Exit()
        {
            base.Exit();
            _holdTimer = 0f;
            _lastHoldDirection = Vector2.zero;
        }
    }
}
