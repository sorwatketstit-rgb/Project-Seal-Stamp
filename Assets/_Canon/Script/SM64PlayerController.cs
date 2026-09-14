using UnityEngine;

namespace SM64
{
    public enum MovementSubState
    {
        Walking,
        Running
    }

    /// <summary>
    /// SM64-style character controller powered by a modular Finite State Machine.
    /// Handles grounded movement (Walking and Running sub-states), responsive double jumping,
    /// long-jumps, backflips, wall-jumps, wall-sliding with cling, ledge-grabbing, and ground-pounds.
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(SM64PlayerInput))]
    public class SM64PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        public float walkSpeed = 3.5f;
        public float runSpeed = 7.0f;
        public float acceleration = 12f;
        public float deceleration = 16f;
        public float turnSmoothTime = 0.08f;

        [Header("Running Mechanics")]
        [Tooltip("Continuous hold time (seconds) in 1 direction required to activate running.")]
        public float runActivationTime = 3.0f;
        [Tooltip("Rapid braking deceleration rate when movement keys are released in Running state.")]
        public float rapidDeceleration = 32.0f;
        [Tooltip("Rate at which horizontal speed gradually decays while in the air.")]
        public float airSpeedDecay = 1.2f;

        [Header("Jumping & Double Jump")]
        public float jumpForce = 7.0f;
        public float doubleJumpForce = 6.5f;
        public float tripleJumpForce = 9.0f;
        public float longJumpForce = 8.5f;
        public float backflipForce = 8.0f;
        public float wallJumpForce = 8.0f;
        public float gravity = -9.81f;
        public float maxFallSpeed = -20f;

        [Header("Double Jump Optimization & Responsiveness")]
        [Tooltip("Number of jumps allowed in mid-air (1 = double jump, 2 = triple jump)")]
        public int maxAirJumps = 1;
        [Tooltip("Grace period (seconds) to jump after running off an edge")]
        public float coyoteTime = 0.15f;
        [Tooltip("Window (seconds) to buffer a jump input prior to landing")]
        public float jumpBufferTime = 0.15f;

        [Header("Wall Slide & Cling Settings")]
        [Tooltip("Layer considered as wall surfaces for sliding and ledge grabbing.")]
        public LayerMask wallLayerMask;
        [Tooltip("Duration in seconds the character clings to the wall before sliding down.")]
        public float wallClingDuration = 0.35f;
        [Tooltip("Downward speed while sliding down a wall.")]
        public float wallSlideSpeed = 2.0f;

        [Header("Ledge Grab Settings")]
        [Tooltip("Offset relative to the detected ledge edge where the character hangs (X=lateral, Y=height below edge, Z=distance from wall).")]
        public Vector3 ledgeHangOffset = new Vector3(0f, -1.0f, 0.35f);

        [Header("Sensor Settings (Top & Middle Dots)")]
        [Tooltip("Forward ray distance for the top and middle wall sensors.")]
        public float sensorDistance = 0.65f;
        [Tooltip("Margin from the very top of the collider for the upper sensor dot.")]
        public float topSensorMargin = 0.06f;

        [Header("Ground / Collision")]
        public float groundSnapDistance = 0.2f;
        public LayerMask groundMask = ~0;
        public float wallCheckDistance = 0.6f;
        public float airControl = 0.4f;

        [Header("State Machine Debug")]
        [SerializeField] private string activeState;
        [SerializeField] private MovementSubState currentMovementSubState = MovementSubState.Walking;

        // References & State Machine
        public CharacterController CharacterController { get; private set; }
        public SM64PlayerInput Input { get; private set; }
        public PlayerStateMachine StateMachine { get; private set; }

        // States
        public PlayerWalkingState WalkingState { get; private set; }
        public PlayerRunningState RunningState { get; private set; }
        public PlayerGroundedState GroundedState => WalkingState; // Backwards compatibility alias
        public PlayerAirborneState AirborneState { get; private set; }
        public PlayerWallSlideState WallSlideState { get; private set; }
        public PlayerLedgeGrabState LedgeGrabState { get; private set; }
        public PlayerLongJumpState LongJumpState { get; private set; }
        public PlayerBackflipState BackflipState { get; private set; }
        public PlayerWallJumpState WallJumpState { get; private set; }
        public PlayerGroundPoundState GroundPoundState { get; private set; }

        // Movement Sub-State
        public MovementSubState CurrentMovementSubState
        {
            get => currentMovementSubState;
            set => currentMovementSubState = value;
        }

        public float RunLandingThreshold => walkSpeed * 1.5f;

        // Runtime Velocity & Counters
        public Vector3 HorizontalVelocity { get; set; }
        public float VerticalVelocity { get; set; }
        public int AirJumpsRemaining { get; set; }

        private float _jumpBufferCounter;
        private float _turnSmoothVelocity;

        private void Awake()
        {
            CharacterController = GetComponent<CharacterController>();
            Input = GetComponent<SM64PlayerInput>();
            if (Input == null)
            {
                Input = gameObject.AddComponent<SM64PlayerInput>();
            }

            // Default to "Wall" layer (Layer 6 in TagManager) if not assigned
            if (wallLayerMask.value == 0)
            {
                int wallLayer = LayerMask.NameToLayer("Wall");
                wallLayerMask = wallLayer != -1 ? (1 << wallLayer) : (1 << 6);
            }

            // Initialize State Machine and concrete states
            StateMachine = new PlayerStateMachine();
            WalkingState = new PlayerWalkingState(this, StateMachine);
            RunningState = new PlayerRunningState(this, StateMachine);
            AirborneState = new PlayerAirborneState(this, StateMachine);
            WallSlideState = new PlayerWallSlideState(this, StateMachine);
            LedgeGrabState = new PlayerLedgeGrabState(this, StateMachine);
            LongJumpState = new PlayerLongJumpState(this, StateMachine);
            BackflipState = new PlayerBackflipState(this, StateMachine);
            WallJumpState = new PlayerWallJumpState(this, StateMachine);
            GroundPoundState = new PlayerGroundPoundState(this, StateMachine);

            StateMachine.OnStateChanged += state => activeState = state.GetType().Name;
        }

        private void Start()
        {
            StateMachine.Initialize(WalkingState);
        }

        private void Update()
        {
            // Jump buffer timer
            if (_jumpBufferCounter > 0f)
            {
                _jumpBufferCounter -= Time.deltaTime;
            }

            // State Machine frame execution
            StateMachine.HandleInput();
            StateMachine.LogicUpdate();
            StateMachine.PhysicsUpdate();

            // Execute movement through CharacterController if enabled
            if (CharacterController.enabled)
            {
                Vector3 finalVelocity = new Vector3(HorizontalVelocity.x, VerticalVelocity, HorizontalVelocity.z);
                CharacterController.Move(finalVelocity * Time.deltaTime);
            }
        }

        /// <summary>
        /// Evaluates whether landing from airborne keeps Running or returns to Walking
        /// based on horizontal speed >= (walkSpeed + half of walkSpeed = 1.5 * walkSpeed).
        /// </summary>
        public IPlayerState GetLandingMovementState()
        {
            float currentSpeed = HorizontalVelocity.magnitude;
            if (CurrentMovementSubState == MovementSubState.Running && currentSpeed >= RunLandingThreshold)
            {
                return RunningState;
            }

            CurrentMovementSubState = MovementSubState.Walking;
            return WalkingState;
        }

        #region Wall & Ledge Sensor Detection

        public Vector3 GetTopSensorOrigin()
        {
            float halfHeight = CharacterController != null ? CharacterController.height * 0.5f : 1.0f;
            float centerY = CharacterController != null ? CharacterController.center.y : 0f;
            return transform.position + Vector3.up * (centerY + halfHeight - topSensorMargin);
        }

        public Vector3 GetMiddleSensorOrigin()
        {
            float centerY = CharacterController != null ? CharacterController.center.y : 0f;
            return transform.position + Vector3.up * centerY;
        }

        public bool CheckWallSensors(out bool middleHit, out bool topHit, out RaycastHit middleHitInfo, out RaycastHit topHitInfo)
        {
            Vector3 topOrigin = GetTopSensorOrigin();
            Vector3 middleOrigin = GetMiddleSensorOrigin();
            Vector3 castDir = transform.forward;

            topHit = Physics.Raycast(topOrigin, castDir, out topHitInfo, sensorDistance, wallLayerMask, QueryTriggerInteraction.Ignore);
            middleHit = Physics.Raycast(middleOrigin, castDir, out middleHitInfo, sensorDistance, wallLayerMask, QueryTriggerInteraction.Ignore);

            return middleHit || topHit;
        }

        public bool FindLedgePositions(RaycastHit middleHitInfo, out Vector3 hangPos, out Vector3 climbPos, out Vector3 wallNormal)
        {
            wallNormal = middleHitInfo.normal;

            // 1. Check if the wall has an explicit LedgeMarker plane attached
            LedgeMarker marker = middleHitInfo.collider.GetComponentInParent<LedgeMarker>() ??
                                 middleHitInfo.collider.GetComponentInChildren<LedgeMarker>();
            if (marker != null)
            {
                hangPos = marker.GetHangPosition();
                climbPos = marker.GetClimbPosition();
                wallNormal = marker.GetWallNormal();
                return true;
            }

            // 2. Automatic geometric edge finding
            Vector3 topOrigin = GetTopSensorOrigin();
            Vector3 downCastOrigin = topOrigin + transform.forward * (middleHitInfo.distance + 0.12f) + Vector3.up * 0.2f;

            if (Physics.Raycast(downCastOrigin, Vector3.down, out RaycastHit topSurfaceHit, CharacterController.height * 0.8f, wallLayerMask, QueryTriggerInteraction.Ignore))
            {
                float edgeY = topSurfaceHit.point.y;
                Vector3 edgePoint = new Vector3(middleHitInfo.point.x, edgeY, middleHitInfo.point.z);

                hangPos = edgePoint + wallNormal * ledgeHangOffset.z + Vector3.up * ledgeHangOffset.y;
                climbPos = topSurfaceHit.point + Vector3.up * (CharacterController.height * 0.5f) + (-wallNormal * 0.4f);
                return true;
            }

            hangPos = Vector3.zero;
            climbPos = Vector3.zero;
            return false;
        }

        #endregion

        #region Helpers & Physics Utilities

        public void ResetAirJumps()
        {
            AirJumpsRemaining = maxAirJumps;
        }

        public void BufferJump()
        {
            _jumpBufferCounter = jumpBufferTime;
        }

        public bool ConsumeBufferedJump()
        {
            if (_jumpBufferCounter > 0f)
            {
                _jumpBufferCounter = 0f;
                return true;
            }
            return false;
        }

        public bool IsGrounded()
        {
            if (CharacterController.isGrounded)
                return true;

            return Physics.Raycast(transform.position, Vector3.down, groundSnapDistance + CharacterController.skinWidth, groundMask, QueryTriggerInteraction.Ignore);
        }

        public bool CheckWall(out Vector3 wallNormal)
        {
            Vector3 origin = transform.position + Vector3.up * (CharacterController.height * 0.5f);
            Vector3[] directions = { transform.forward, -transform.forward, transform.right, -transform.right };

            foreach (var dir in directions)
            {
                if (Physics.Raycast(origin, dir, out RaycastHit hit, wallCheckDistance, groundMask, QueryTriggerInteraction.Ignore))
                {
                    wallNormal = hit.normal;
                    return true;
                }
            }

            wallNormal = Vector3.zero;
            return false;
        }

        public Vector3 GetCameraRelativeInput(Vector2 moveInput)
        {
            if (moveInput.sqrMagnitude < 0.001f)
                return Vector3.zero;

            Transform cam = Camera.main != null ? Camera.main.transform : null;
            Vector3 camForward = cam ? Vector3.Scale(cam.forward, new Vector3(1, 0, 1)).normalized : Vector3.forward;
            Vector3 camRight = cam ? Vector3.Scale(cam.right, new Vector3(1, 0, 1)).normalized : Vector3.right;

            return (camForward * moveInput.y + camRight * moveInput.x).normalized;
        }

        public void RotateTowards(Vector3 direction, float smoothTime)
        {
            if (direction.sqrMagnitude < 0.001f)
                return;

            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, smoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
        }

        public void ApplyGravity(float multiplier = 1f)
        {
            if (IsGrounded() && VerticalVelocity < 0f)
            {
                VerticalVelocity = -2f;
                return;
            }

            VerticalVelocity = Mathf.Max(VerticalVelocity + gravity * multiplier * Time.deltaTime, maxFallSpeed);
        }

        #endregion

        #region Scene Gizmos

        private void OnDrawGizmosSelected()
        {
            Vector3 topOrigin = GetTopSensorOrigin();
            Vector3 middleOrigin = GetMiddleSensorOrigin();

            // Draw Middle Sensor Dot & Ray
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(middleOrigin, 0.06f);
            Gizmos.DrawRay(middleOrigin, transform.forward * sensorDistance);

            // Draw Top Sensor Dot & Ray
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(topOrigin, 0.06f);
            Gizmos.DrawRay(topOrigin, transform.forward * sensorDistance);
        }

        #endregion
    }
}
