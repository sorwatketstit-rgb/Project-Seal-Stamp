using UnityEngine;

namespace SM64
{
    /// <summary>
    /// Grounded movement state for Running.
    /// Moves at high speed with full strafing steering. Releasing movement keys rapidly
    /// brakes velocity to 0 and transitions back to WalkingState.
    /// </summary>
    public class PlayerRunningState : PlayerGroundedState
    {
        public PlayerRunningState(SM64PlayerController controller, PlayerStateMachine stateMachine) 
            : base(controller, stateMachine) { }

        public override void Enter()
        {
            base.Enter();
            Controller.CurrentMovementSubState = MovementSubState.Running;
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
                // Rapid brake to zero when keys are released
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, Controller.rapidDeceleration * Time.deltaTime);
                Controller.HorizontalVelocity = (currentSpeed > 0.05f) ? Controller.transform.forward * currentSpeed : Vector3.zero;

                if (currentSpeed <= 0.05f)
                {
                    StateMachine.ChangeState(Controller.WalkingState);
                }
            }
        }
    }
}
