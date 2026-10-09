using UnityEngine;

namespace SM64
{
    /// <summary>
    /// Crouching State — player moves at half speed and cannot accelerate into running.
    /// Temporarily swaps the player model to the inspector-assigned crouch model.
    /// The only allowed action while crouching is pressing Jump (Crouch + Space) to turn into a Seal.
    /// Releasing Crouch returns the player to the grounded walking state.
    /// </summary>
    public class PlayerCrouchingState : PlayerState
    {
        public PlayerCrouchingState(SM64PlayerController controller, PlayerStateMachine stateMachine)
            : base(controller, stateMachine) { }

        public override void Enter()
        {
            // [TEMPORARY] Swap to crouch model
            Controller.SetVisualModel(PlayerVisualModel.Crouch);

            Controller.AirborneState.ResetJumpCount();

            if (Controller.IsGrounded())
            {
                Controller.VerticalVelocity = -2f;
            }
        }

        public override void HandleInput()
        {
            if (Input == null) return;

            // Crouch + Jump (Ctrl + Space) -> Turn into Seal
            if (Input.JumpPressed)
            {
                StateMachine.ChangeState(Controller.SealState);
                return;
            }

            // Release crouch to stand back up
            if (!Input.CrouchHeld)
            {
                StateMachine.ChangeState(Controller.WalkingState);
                return;
            }
        }

        public override void LogicUpdate()
        {
            // Stepping off a ledge into the air
            if (!Controller.IsGrounded())
            {
                Controller.AirborneState.SetupFallWithCoyoteTime();
                StateMachine.ChangeState(Controller.AirborneState);
            }
        }

        public override void PhysicsUpdate()
        {
            // Half walk speed, strictly no acceleration into running
            float targetWalkSpeed = Controller.CrouchSpeed;

            Vector3 desiredDir = Controller.GetCameraRelativeInput(Input != null ? Input.MoveInput : Vector2.zero);
            float targetSpeed = desiredDir.magnitude > 0.1f ? targetWalkSpeed : 0f;

            // Direct smooth speed clamping
            Vector3 currentHoriz = Controller.HorizontalVelocity;
            float currentSpeed = currentHoriz.magnitude;
            float accel = (targetSpeed > currentSpeed) ? Controller.acceleration : Controller.deceleration;
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accel * Time.deltaTime);

            if (desiredDir.sqrMagnitude > 0.001f)
            {
                Controller.RotateTowards(desiredDir, Controller.turnSmoothTime);
            }

            Vector3 newHoriz = (currentSpeed > 0.001f) ? Controller.transform.forward * currentSpeed : Vector3.zero;
            Controller.HorizontalVelocity = newHoriz;

            Controller.ApplyGravity();
        }

        public override void Exit()
        {
            // [TEMPORARY] Revert model back to default
            Controller.SetVisualModel(PlayerVisualModel.Default);
        }
    }
}
