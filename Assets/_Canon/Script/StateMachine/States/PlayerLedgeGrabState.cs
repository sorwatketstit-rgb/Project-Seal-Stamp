using UnityEngine;

namespace SM64
{
    /// <summary>
    /// State when middle sensor detects the wall but upper sensor is clear (an edge/ledge is reached).
    /// Character hangs from the ledge with support for climb-up or drop-down.
    /// </summary>
    public class PlayerLedgeGrabState : PlayerState
    {
        private Vector3 _hangPosition;
        private Vector3 _climbPosition;
        private Vector3 _wallNormal;

        private bool _isClimbing;
        private float _climbProgress;
        private Vector3 _climbStartPos;
        private const float ClimbDuration = 0.28f;

        public float LedgeCooldownTimer { get; set; }

        public PlayerLedgeGrabState(SM64PlayerController controller, PlayerStateMachine stateMachine) 
            : base(controller, stateMachine) { }

        public void SetupLedge(Vector3 hangPos, Vector3 climbPos, Vector3 wallNormal)
        {
            _hangPosition = hangPos;
            _climbPosition = climbPos;
            _wallNormal = wallNormal;
        }

        public override void Enter()
        {
            Controller.ResetAirJumps();
            Controller.VerticalVelocity = 0f;
            Controller.HorizontalVelocity = Vector3.zero;
            Controller.CurrentMovementSubState = MovementSubState.Walking;

            _isClimbing = false;
            _climbProgress = 0f;

            // Orient the character facing the wall/ledge
            if (_wallNormal.sqrMagnitude > 0.001f)
            {
                Vector3 lookDir = -_wallNormal;
                lookDir.y = 0f;
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    Controller.transform.rotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
                }
            }

            // Snap character into hanging position
            CharController.enabled = false;
            Controller.transform.position = _hangPosition;
            CharController.enabled = true;
        }

        public override void HandleInput()
        {
            if (Input == null || _isClimbing) return;

            Vector3 desiredDir = Controller.GetCameraRelativeInput(Input.MoveInput);

            // 1. Drop down (Crouch or pulling away)
            if (Input.CrouchPressed)
            {
                DropFromLedge();
                return;
            }

            if (desiredDir.sqrMagnitude > 0.3f)
            {
                float awayDot = Vector3.Dot(desiredDir.normalized, _wallNormal);
                if (awayDot > 0.5f) // Pulling backward away from ledge
                {
                    if (Input.JumpPressed)
                    {
                        // Leap backwards away from ledge
                        Controller.WallJumpState.SetWallNormal(_wallNormal);
                        StateMachine.ChangeState(Controller.WallJumpState);
                        return;
                    }

                    DropFromLedge();
                    return;
                }
            }

            // 2. Climb up (Jump or pushing forward into the ledge)
            bool pushForward = desiredDir.sqrMagnitude > 0.3f && Vector3.Dot(desiredDir.normalized, -_wallNormal) > 0.4f;
            if (Input.JumpPressed || pushForward)
            {
                StartClimbUp();
            }
        }

        private void StartClimbUp()
        {
            _isClimbing = true;
            _climbProgress = 0f;
            _climbStartPos = Controller.transform.position;
            CharController.enabled = false;
        }

        private void DropFromLedge()
        {
            LedgeCooldownTimer = 0.4f; // Prevent instantly re-grabbing the same ledge
            Controller.VerticalVelocity = -1.5f;
            Controller.HorizontalVelocity = _wallNormal * (Controller.walkSpeed * 0.4f);
            StateMachine.ChangeState(Controller.AirborneState);
        }

        public override void LogicUpdate()
        {
            if (_isClimbing)
            {
                _climbProgress += Time.deltaTime / ClimbDuration;

                // Smooth curved arc: up first, then forward onto the surface
                float t = Mathf.Clamp01(_climbProgress);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                Vector3 midPoint = new Vector3(_climbStartPos.x, _climbPosition.y, _climbStartPos.z);
                Vector3 currentTarget;
                if (t < 0.5f)
                {
                    currentTarget = Vector3.Lerp(_climbStartPos, midPoint, t * 2f);
                }
                else
                {
                    currentTarget = Vector3.Lerp(midPoint, _climbPosition, (t - 0.5f) * 2f);
                }

                Controller.transform.position = currentTarget;

                if (_climbProgress >= 1f)
                {
                    Controller.transform.position = _climbPosition;
                    CharController.enabled = true;
                    _isClimbing = false;
                    StateMachine.ChangeState(Controller.WalkingState);
                }
            }
        }

        public override void PhysicsUpdate()
        {
            if (!_isClimbing)
            {
                // Lock velocity while hanging
                Controller.VerticalVelocity = 0f;
                Controller.HorizontalVelocity = Vector3.zero;
            }
        }

        public override void Exit()
        {
            CharController.enabled = true;
            _isClimbing = false;
        }
    }
}
