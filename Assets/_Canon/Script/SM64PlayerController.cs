using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SM64
{
    /// <summary>
    /// Main character controller implementing SM64‑style movement,
    /// jumps, long‑jumps, back‑flips, wall‑jumps, and ground snap mechanics.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class SM64PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        public float walkSpeed = 3.5f;
        public float runSpeed = 7.0f;
        public float acceleration = 12f;      // how fast you reach target speed
        public float deceleration = 16f;      // how fast you stop
        public float turnSmoothTime = 0.08f;  // turning inertia

        [Header("Jumping")]
        public float jumpForce = 7.0f;
        public float doubleJumpForce = 6.5f;
        public float tripleJumpForce = 9.0f;
        public float longJumpForce = 8.5f;
        public float backflipForce = 8.0f;
        public float wallJumpForce = 8.0f;
        public float gravity = -9.81f;
        public float maxFallSpeed = -20f;

        [Header("Advanced")]
        public float groundSnapDistance = 0.2f;   // keep you glued to ground
        public LayerMask groundMask = ~0;         // what counts as ground
        public float wallCheckDistance = 0.6f;    // for wall‑jump detection
        public float airControl = 0.4f;           // how responsive you are mid‑air

        private CharacterController _controller;
        private SM64PlayerInput _input;
        private Vector3 _velocity;               // current movement velocity
        private Vector3 _desiredMovement;        // input‑driven direction
        private float _yVelocity;                // vertical component
        private int _jumpCount;                  // 0 = grounded, 1 = single, 2 = double
        private bool _isLongJumpPrep;
        private bool _isBackflipPrep;
        private bool _isGroundPounding;
        private float _turnSmoothVelocity;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<SM64PlayerInput>();
            if (_input == null)
            {
                Debug.LogError("SM64PlayerInput component missing on Player.");
            }
        }

        private void Update()
        {
            HandleMovement();
            HandleJumping();
            ApplyGravity();
            _controller.Move(_velocity * Time.deltaTime);
        }

        private bool IsGrounded()
        {
            if (_controller.isGrounded)
                return true;
            return Physics.Raycast(transform.position, Vector3.down, groundSnapDistance, groundMask);
        }

        private bool IsTouchingWall(out Vector3 wallNormal)
        {
            Vector3[] dirs = { transform.right, -transform.right };
            foreach (var dir in dirs)
            {
                if (Physics.Raycast(transform.position, dir, out RaycastHit hit, wallCheckDistance, groundMask))
                {
                    wallNormal = hit.normal;
                    return true;
                }
            }
            wallNormal = Vector3.zero;
            return false;
        }

        private void HandleMovement()
        {
            // Determine target speed
            float targetSpeed = _input.MoveInput.magnitude > 0.1f
                ? (_input.CrouchHeld ? walkSpeed : runSpeed)
                : 0f;

            // Desired horizontal direction relative to main camera
            Transform cam = Camera.main != null ? Camera.main.transform : null;
            Vector3 camForward = cam ? Vector3.Scale(cam.forward, new Vector3(1, 0, 1)).normalized : Vector3.forward;
            Vector3 camRight = cam ? Vector3.Scale(cam.right, new Vector3(1, 0, 1)).normalized : Vector3.right;

            _desiredMovement = (camForward * _input.MoveInput.y + camRight * _input.MoveInput.x).normalized * targetSpeed;

            // Smooth acceleration / deceleration
            float currentHorizontalSpeed = new Vector3(_velocity.x, 0, _velocity.z).magnitude;
            float accel = (targetSpeed > currentHorizontalSpeed) ? acceleration : deceleration;
            currentHorizontalSpeed = Mathf.MoveTowards(currentHorizontalSpeed, targetSpeed, accel * Time.deltaTime);

            // Turn smoothing
            if (_desiredMovement.sqrMagnitude > 0.0001f)
            {
                float targetAngle = Mathf.Atan2(_desiredMovement.x, _desiredMovement.z) * Mathf.Rad2Deg;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, turnSmoothTime);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);
            }

            Vector3 horizontal = transform.forward * currentHorizontalSpeed;
            _velocity = new Vector3(horizontal.x, _yVelocity, horizontal.z);
        }

        private void HandleJumping()
        {
            // Reset jump states on ground impact
            if (IsGrounded())
            {
                _jumpCount = 0;
                _isLongJumpPrep = _isBackflipPrep = _isGroundPounding = false;
            }

            // Prep triggers
            if (_input.CrouchPressed && IsGrounded())
            {
                _isLongJumpPrep = true;
                _isBackflipPrep = _desiredMovement.sqrMagnitude < 0.01f;
            }

            // Jump handling
            if (_input.JumpPressed)
            {
                if (_isLongJumpPrep)
                {
                    _yVelocity = longJumpForce;
                    _velocity += transform.forward * runSpeed * 1.2f;
                    _isLongJumpPrep = false;
                }
                else if (_isBackflipPrep)
                {
                    _yVelocity = backflipForce;
                    _isBackflipPrep = false;
                }
                else if (IsGrounded())
                {
                    _yVelocity = jumpForce;
                    _jumpCount = 1;
                }
                else if (_jumpCount == 1 && !_isGroundPounding)
                {
                    _yVelocity = doubleJumpForce;
                    _jumpCount = 2;
                }
                else if (_jumpCount == 2 && !_isGroundPounding)
                {
                    _yVelocity = tripleJumpForce;
                }
            }
        }

        private void ApplyGravity()
        {
            if (IsGrounded() && _yVelocity < 0f)
            {
                _yVelocity = -2f; // keeps player grounded
                return;
            }

            // Wall jump check
            if (!IsGrounded() && _input.JumpPressed && IsTouchingWall(out Vector3 wallNormal))
            {
                Vector3 away = (wallNormal + Vector3.up).normalized;
                _yVelocity = wallJumpForce;
                _velocity += away * wallJumpForce;
            }

            _yVelocity = Mathf.Max(_yVelocity + gravity * Time.deltaTime, maxFallSpeed);
        }
    }
}
