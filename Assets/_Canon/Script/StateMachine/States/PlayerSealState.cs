using UnityEngine;

namespace SM64
{
    /// <summary>
    /// Seal State — temporarily transforms the player into a seal model with visual effects.
    /// Triggered exclusively:
    /// 1. After leaping, if the player does not touch the ground within 1 second.
    /// 2. When pressing Jump while crouching (Crouch + Space bar).
    /// Functions similarly to crouching for movement: half speed, no running acceleration, walk only.
    /// Pressing Space (Jump) is how the player reverts back to normal (not Ctrl).
    /// </summary>
    public class PlayerSealState : PlayerState
    {
        public PlayerSealState(SM64PlayerController controller, PlayerStateMachine stateMachine)
            : base(controller, stateMachine) { }

        public override void Enter()
        {
            // [TEMPORARY] Swap to seal model
            Controller.SetVisualModel(PlayerVisualModel.Seal);

            // Play transformation particle effect
            Controller.PlaySealTransformParticle();

            if (Controller.IsGrounded())
            {
                Controller.VerticalVelocity = -2f;
            }
        }

        public override void HandleInput()
        {
            if (Input == null) return;

            // In seal state, pressing Space (Jump) reverts the player back to normal
            if (Input.JumpPressed)
            {
                RevertToNormal();
                return;
            }
        }

        public override void LogicUpdate()
        {
            // Landing check if entered mid-air (from leap timeout)
            if (Controller.IsGrounded() && Controller.VerticalVelocity <= 0f)
            {
                Controller.VerticalVelocity = -2f;
                Controller.AirborneState.ResetJumpCount();
            }
        }

        public override void PhysicsUpdate()
        {
            // Apply gravity regardless of whether grounded or mid-air
            Controller.ApplyGravity();

            // Half walk speed, strictly no running acceleration (same as crouching)
            float targetSpeedCap = Controller.CrouchSpeed;

            Vector3 desiredDir = Controller.GetCameraRelativeInput(Input != null ? Input.MoveInput : Vector2.zero);
            float targetSpeed = desiredDir.magnitude > 0.1f ? targetSpeedCap : 0f;

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
        }

        public override void Exit()
        {
            // [TEMPORARY] Revert model back to default
            Controller.SetVisualModel(PlayerVisualModel.Default);

            // Play particle effect when reverting back from seal state
            Controller.PlaySealRevertParticle();
        }

        private void RevertToNormal()
        {
            if (Controller.IsGrounded())
            {
                StateMachine.ChangeState(Controller.WalkingState);
            }
            else
            {
                StateMachine.ChangeState(Controller.AirborneState);
            }
        }
    }
}
