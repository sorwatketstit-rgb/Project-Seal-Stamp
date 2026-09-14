using UnityEngine;

namespace SM64
{
    /// <summary>
    /// State when both upper and middle sensors touch a "Wall" layer surface.
    /// Character clings to the wall briefly, then descends at a controlled slide speed.
    /// </summary>
    public class PlayerWallSlideState : PlayerState
    {
        private Vector3 _wallNormal;
        private float _clingTimer;

        public PlayerWallSlideState(SM64PlayerController controller, PlayerStateMachine stateMachine) 
            : base(controller, stateMachine) { }

        public void SetWallContact(Vector3 wallNormal)
        {
            _wallNormal = wallNormal;
        }

        public override void Enter()
        {
            // Reset mid-air jump charges when successfully latching to a wall
            Controller.ResetAirJumps();

            _clingTimer = Controller.wallClingDuration;
            Controller.VerticalVelocity = 0f;
            Controller.HorizontalVelocity = Vector3.zero;

            // Orient the player to face the wall surface
            if (_wallNormal.sqrMagnitude > 0.001f)
            {
                Vector3 lookDir = -_wallNormal;
                lookDir.y = 0f;
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    Controller.transform.rotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
                }
            }
        }

        public override void HandleInput()
        {
            if (Input == null) return;

            // Wall Jump off the sliding surface
            if (Input.JumpPressed)
            {
                Controller.WallJumpState.SetWallNormal(_wallNormal);
                StateMachine.ChangeState(Controller.WallJumpState);
                return;
            }

            // Drop off wall using crouch
            if (Input.CrouchPressed)
            {
                StateMachine.ChangeState(Controller.AirborneState);
                return;
            }

            // Pulling away from wall with movement stick
            Vector3 desiredDir = Controller.GetCameraRelativeInput(Input.MoveInput);
            if (desiredDir.sqrMagnitude > 0.2f)
            {
                float awayDot = Vector3.Dot(desiredDir.normalized, _wallNormal);
                if (awayDot > 0.6f) // Player actively pushing away from wall
                {
                    Controller.HorizontalVelocity = _wallNormal * (Controller.walkSpeed * 0.7f);
                    StateMachine.ChangeState(Controller.AirborneState);
                }
            }
        }

        public override void LogicUpdate()
        {
            // Landing check
            if (Controller.IsGrounded())
            {
                Controller.CurrentMovementSubState = MovementSubState.Walking;
                StateMachine.ChangeState(Controller.WalkingState);
                return;
            }

            // Re-check wall contact with sensors
            bool hasMiddle = Controller.CheckWallSensors(out bool middleHit, out bool topHit, out RaycastHit middleHitInfo, out RaycastHit topHitInfo);

            if (!middleHit && !topHit)
            {
                // Lost wall contact entirely
                StateMachine.ChangeState(Controller.AirborneState);
                return;
            }

            if (middleHit)
            {
                _wallNormal = middleHitInfo.normal;
            }

            // Cling countdown
            if (_clingTimer > 0f)
            {
                _clingTimer -= Time.deltaTime;
                Controller.VerticalVelocity = 0f;
            }
            else
            {
                // Controlled slow descent
                Controller.VerticalVelocity = -Controller.wallSlideSpeed;
            }
        }

        public override void PhysicsUpdate()
        {
            // Gently push slightly into wall to maintain CharacterController contact
            Controller.HorizontalVelocity = -_wallNormal * 0.1f;
        }

        public override void Exit()
        {
            _clingTimer = 0f;
        }
    }
}
