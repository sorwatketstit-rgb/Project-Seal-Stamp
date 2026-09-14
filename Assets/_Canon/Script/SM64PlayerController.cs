using UnityEngine;

namespace SM64
{
    /// <summary>
    /// SM64-style character controller powered by a modular Finite State Machine.
    /// Handles grounded movement, responsive double jumping, long-jumps, backflips,
    /// wall-jumps, and ground-pounds.
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

        [Header("Advanced / Collision")]
        public float groundSnapDistance = 0.2f;
        public LayerMask groundMask = ~0;
        public float wallCheckDistance = 0.6f;
        public float airControl = 0.4f;

        [Header("State Machine Debug")]
        [SerializeField] private string activeState;

        // References & State Machine
        public CharacterController CharacterController { get; private set; }
        public SM64PlayerInput Input { get; private set; }
        public PlayerStateMachine StateMachine { get; private set; }

        // States
        public PlayerGroundedState GroundedState { get; private set; }
        public PlayerAirborneState AirborneState { get; private set; }
        public PlayerLongJumpState LongJumpState { get; private set; }
        public PlayerBackflipState BackflipState { get; private set; }
        public PlayerWallJumpState WallJumpState { get; private set; }
        public PlayerGroundPoundState GroundPoundState { get; private set; }

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

            // Initialize State Machine and concrete states
            StateMachine = new PlayerStateMachine();
            GroundedState = new PlayerGroundedState(this, StateMachine);
            AirborneState = new PlayerAirborneState(this, StateMachine);
            LongJumpState = new PlayerLongJumpState(this, StateMachine);
            BackflipState = new PlayerBackflipState(this, StateMachine);
            WallJumpState = new PlayerWallJumpState(this, StateMachine);
            GroundPoundState = new PlayerGroundPoundState(this, StateMachine);

            StateMachine.OnStateChanged += state => activeState = state.GetType().Name;
        }

        private void Start()
        {
            StateMachine.Initialize(GroundedState);
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

            // Execute movement through CharacterController
            Vector3 finalVelocity = new Vector3(HorizontalVelocity.x, VerticalVelocity, HorizontalVelocity.z);
            CharacterController.Move(finalVelocity * Time.deltaTime);
        }

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

            // Extra ground check using sphere/raycast for snappy step-downs
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
    }
}
