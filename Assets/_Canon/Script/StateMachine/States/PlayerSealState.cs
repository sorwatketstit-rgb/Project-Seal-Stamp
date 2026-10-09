using UnityEngine;

namespace SM64
{
    /// <summary>
    /// Seal State — temporarily transforms the player into a seal model.
    /// Triggered exclusively:
    /// 1. After leaping, if the player does not touch the ground within 1 second.
    /// 2. When pressing Jump while crouching (Crouch + Space bar).
    /// For now, functions identically to crouching: half speed, no running acceleration, only walk.
    /// Pressing Crouch again (or releasing Crouch if entered from crouch hold) reverts the player to normal.
    /// </summary>
    public class PlayerSealState : PlayerState
    {
        private bool _enteredFromCrouchHold;

        public PlayerSealState(SM64PlayerController controller, PlayerStateMachine stateMachine)
            : base(controller, stateMachine) { }

        public override void Enter()
        {
            // [TEMPORARY] Swap to seal model
            Controller.SetVisualModel(PlayerVisualModel.Seal);

            _enteredFromCrouchHold = Input != null && Input.CrouchHeld;

            if (Controller.IsGrounded())
            {
                Controller.VerticalVelocity = -2f;
            }
        }

        public override void HandleInput()
        {
            if (Input == null) return;

            // Revert back from seal if Crouch is pressed again (Ctrl / C)
            if (Input.CrouchPressed)
            {
                RevertToNormal();
                return;
            }

            // If entered via holding Crouch + Jump, releasing Crouch after landing returns to normal
            if (_enteredFromCrouchHold && !Input.CrouchHeld && Controller.IsGrounded())
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
            _enteredFromCrouchHold = false;

            // [TEMPORARY] Revert model back to default
            Controller.SetVisualModel(PlayerVisualModel.Default);
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
