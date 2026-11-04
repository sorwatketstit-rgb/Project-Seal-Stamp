using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SM64
{
    /// <summary>
    /// Third-person camera that follows the player, orbits around it,
    /// avoids clipping through geometry, supports Genshin-style menu camera transitions,
    /// and drives proximity fading for close-up objects.
    /// </summary>
    public class SM64CameraController : MonoBehaviour
    {
        [Header("Targeting")]
        public Transform target;
        public Vector3 targetOffset = new Vector3(0, 1.5f, 0);

        [Header("Orbit Settings")]
        public float rotateSpeed = 120f;
        public float zoomSpeed = 2f;
        public float minDistance = 2f;
        public float maxDistance = 8f;

        [Header("Collision Avoidance")]
        public LayerMask obstructionMask = ~0;
        public float cameraRadius = 0.2f;

        [Header("Proximity Fade Settings")]
        [Tooltip("Enables dither/transparency fading for any object close to the camera.")]
        public bool enableProximityFade = true;
        [Tooltip("Distance at which objects close to the camera become fully transparent / dither clipped.")]
        public float fadeNearDistance = 0.5f;
        [Tooltip("Distance at which objects start fading out as the camera gets closer.")]
        public float fadeFarDistance = 1.6f;

        [Header("Menu Camera State")]
        [SerializeField] private bool isMenuMode = false;
        public bool IsMenuMode => isMenuMode;

        [Header("Aim Mode (TPS Shooter)")]
        [Tooltip("Camera offset from the pivot while in TPS aim mode (over the shoulder).")]
        public Vector3 aimShoulderOffset = new Vector3(0.65f, 0.2f, -1.8f);
        [Tooltip("Field of View to transition to while aiming.")]
        public float aimFieldOfView = 50f;
        public bool enableAimFov = true;
        [Tooltip("Speed of blending between normal orbit and TPS aim mode.")]
        public float aimTransitionSpeed = 10f;
        public float aimRotateSpeed = 120f;
        public bool IsAiming => _isAiming;

        [Header("Aim Mode Compensation & Convergence")]
        [Tooltip("Automatically angles the camera towards the player's central aim line to compensate for any shoulder offset.")]
        public bool enableConvergenceCompensation = true;

        [Tooltip("Distance ahead of the player where the camera crosshair converges.")]
        public float aimConvergenceDistance = 25f;

        [Tooltip("Fine-tune manual compensation offset (X = horizontal, Y = vertical, Z = depth) applied to the aim point.")]
        public Vector3 aimCompensationOffset = Vector3.zero;

        /// <summary>The world position where the camera aim currently converges.</summary>
        public Vector3 CurrentAimTargetPoint { get; private set; }

        private bool _isAiming = false;
        private float _aimBlend = 0f;
        private float _defaultFov = 60f;
        private Camera _cam;

        private float _currentDistance;
        private float _yaw;
        private float _pitch = 15f;
        private SM64PlayerInput _input;

        // Saved state for returning from menu
        private Vector3 _menuSavedPosition;
        private Quaternion _menuSavedRotation;
        private float _menuSavedPitch;
        private float _menuSavedYaw;
        private float _menuSavedDistance;

        // Transition runtime state
        private Vector3 _menuTargetPosition;
        private Vector3 _menuLookAtTarget;
        private Vector3 _transitionStartPosition;
        private Quaternion _transitionStartRotation;
        private float _menuTransitionDuration = 0.5f;
        private float _menuTransitionTimer = 0f;
        private bool _inMenuTransition = false;
        private bool _transitioningToMenu = false;

        private void Awake()
        {
            _currentDistance = maxDistance;
            _cam = GetComponent<Camera>();
            if (_cam != null)
            {
                _defaultFov = _cam.fieldOfView;
            }
        }

        private void Start()
        {
            if (_cam == null)
            {
                _cam = Camera.main;
                if (_cam != null) _defaultFov = _cam.fieldOfView;
            }

            // Acquire input reference from target if available
            if (target != null)
            {
                _input = target.GetComponent<SM64PlayerInput>();
            }

            if (_input == null)
            {
                _input = FindFirstObjectByType<SM64PlayerInput>();
            }
        }

        private void LateUpdate()
        {
            UpdateProximityFadeShaderGlobals();

            if (target == null)
                return;

            if (isMenuMode)
            {
                HandleMenuCameraUpdate();
                return;
            }

            // Fetch input directly from player input reference
            Vector2 look = Vector2.zero;
            if (_input != null)
            {
                look = _input.LookInput;
            }

            float currentRotateSpeed = _isAiming ? aimRotateSpeed : rotateSpeed;

            // Delta values are already frame-rate independent from Mouse.delta
            _yaw += look.x * currentRotateSpeed;
            _pitch -= look.y * currentRotateSpeed;
            _pitch = Mathf.Clamp(_pitch, _isAiming ? -50f : -30f, _isAiming ? 70f : 60f);

            // Handle Zoom Input (only when not aiming)
            if (!_isAiming)
            {
                HandleZoom();
            }

            // Update aim blend (0 = normal, 1 = aim)
            float targetBlend = _isAiming ? 1f : 0f;
            _aimBlend = Mathf.MoveTowards(_aimBlend, targetBlend, Time.deltaTime * aimTransitionSpeed);

            // Calculate pivot point and rotation
            Vector3 pivot = target.position + targetOffset;
            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);

            // 1. Normal orbit position & rotation
            Vector3 normalPos = pivot + rotation * new Vector3(0, 0, -_currentDistance);
            Vector3 normalRayDir = normalPos - pivot;
            float normalRayLen = normalRayDir.magnitude;

            if (normalRayLen > 0.001f && Physics.SphereCast(pivot, cameraRadius, normalRayDir.normalized, out RaycastHit hitNormal, normalRayLen, obstructionMask, QueryTriggerInteraction.Ignore))
            {
                normalPos = pivot + normalRayDir.normalized * Mathf.Max(hitNormal.distance - cameraRadius, 0.1f);
            }
            Vector3 normalLookDir = pivot - normalPos;
            Quaternion normalRot = normalLookDir.sqrMagnitude > 0.001f ? Quaternion.LookRotation(normalLookDir) : rotation;

            // 2. TPS Aim mode position & rotation (over the shoulder)
            Vector3 aimPos = pivot + rotation * aimShoulderOffset;
            Vector3 aimRayDir = aimPos - pivot;
            float aimRayLen = aimRayDir.magnitude;

            if (aimRayLen > 0.001f && Physics.SphereCast(pivot, cameraRadius, aimRayDir.normalized, out RaycastHit hitAim, aimRayLen, obstructionMask, QueryTriggerInteraction.Ignore))
            {
                aimPos = pivot + aimRayDir.normalized * Mathf.Max(hitAim.distance - cameraRadius, 0.1f);
            }

            Quaternion aimRot = rotation;

            if (enableConvergenceCompensation)
            {
                // Target point along the central aim line in front of the player, plus optional fine-tune compensation offset
                Vector3 centralTarget = pivot + rotation * (Vector3.forward * aimConvergenceDistance + aimCompensationOffset);
                CurrentAimTargetPoint = centralTarget;

                Vector3 camToTarget = centralTarget - aimPos;
                if (camToTarget.sqrMagnitude > 0.001f)
                {
                    aimRot = Quaternion.LookRotation(camToTarget.normalized);
                }
            }
            else
            {
                CurrentAimTargetPoint = aimPos + rotation * (Vector3.forward * aimConvergenceDistance + aimCompensationOffset);
            }

            // 3. Smooth blend between normal and aim mode
            transform.position = Vector3.Lerp(normalPos, aimPos, _aimBlend);
            transform.rotation = Quaternion.Slerp(normalRot, aimRot, _aimBlend);

            // Blend FOV
            if (_cam != null && enableAimFov)
            {
                _cam.fieldOfView = Mathf.Lerp(_defaultFov, aimFieldOfView, _aimBlend);
            }
        }

        /// <summary>
        /// Toggles third-person shooter aiming camera mode.
        /// </summary>
        public void SetAimMode(bool aiming)
        {
            _isAiming = aiming;
        }

        private void HandleMenuCameraUpdate()
        {
            if (_inMenuTransition)
            {
                _menuTransitionTimer += Time.deltaTime;
                float t = Mathf.Clamp01(_menuTransitionTimer / Mathf.Max(_menuTransitionDuration, 0.001f));
                // Smooth cubic ease in out
                float smoothT = t * t * (3f - 2f * t);

                if (_transitioningToMenu)
                {
                    transform.position = Vector3.Lerp(_transitionStartPosition, _menuTargetPosition, smoothT);
                    Vector3 lookDir = _menuLookAtTarget - transform.position;
                    if (lookDir.sqrMagnitude > 0.001f)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(lookDir);
                        transform.rotation = Quaternion.Slerp(_transitionStartRotation, targetRot, smoothT);
                    }

                    if (t >= 1f)
                    {
                        _inMenuTransition = false;
                    }
                }
                else
                {
                    // Transitioning back to gameplay
                    transform.position = Vector3.Lerp(_transitionStartPosition, _menuSavedPosition, smoothT);
                    transform.rotation = Quaternion.Slerp(_transitionStartRotation, _menuSavedRotation, smoothT);

                    if (t >= 1f)
                    {
                        _inMenuTransition = false;
                        isMenuMode = false;
                        _pitch = _menuSavedPitch;
                        _yaw = _menuSavedYaw;
                        _currentDistance = _menuSavedDistance;
                    }
                }
            }
            else
            {
                // In menu holding position, look at target pivot
                transform.position = _menuTargetPosition;
                Vector3 lookDir = _menuLookAtTarget - transform.position;
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(lookDir);
                }
            }
        }

        /// <summary>
        /// Saves the camera's current position and transitions smoothly to the chosen menu dot position.
        /// </summary>
        public void StartMenuCamera(Vector3 menuPosition, Vector3 lookAtPosition, float duration = 0.5f)
        {
            _menuSavedPosition = transform.position;
            _menuSavedRotation = transform.rotation;
            _menuSavedPitch = _pitch;
            _menuSavedYaw = _yaw;
            _menuSavedDistance = _currentDistance;

            _transitionStartPosition = transform.position;
            _transitionStartRotation = transform.rotation;
            _menuTargetPosition = menuPosition;
            _menuLookAtTarget = lookAtPosition;
            _menuTransitionDuration = duration;
            _menuTransitionTimer = 0f;
            _inMenuTransition = true;
            _transitioningToMenu = true;
            isMenuMode = true;
        }

        /// <summary>
        /// Transitions smoothly back from the menu camera position to the saved gameplay camera position.
        /// </summary>
        public void EndMenuCamera(float duration = 0.5f)
        {
            if (!isMenuMode) return;

            _transitionStartPosition = transform.position;
            _transitionStartRotation = transform.rotation;
            _menuTransitionDuration = duration;
            _menuTransitionTimer = 0f;
            _inMenuTransition = true;
            _transitioningToMenu = false;
        }

        private void UpdateProximityFadeShaderGlobals()
        {
            if (enableProximityFade)
            {
                Shader.SetGlobalFloat("_GlobalCameraFadeNear", fadeNearDistance);
                Shader.SetGlobalFloat("_GlobalCameraFadeFar", fadeFarDistance);
            }
            else
            {
                Shader.SetGlobalFloat("_GlobalCameraFadeNear", -1f);
                Shader.SetGlobalFloat("_GlobalCameraFadeFar", -1f);
            }
        }

        private void HandleZoom()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                float scroll = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    _currentDistance -= Mathf.Sign(scroll) * zoomSpeed;
                }
            }
#else
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _currentDistance -= scroll * zoomSpeed * 10f;
            }
#endif
            _currentDistance = Mathf.Clamp(_currentDistance, minDistance, maxDistance);
        }
    }
}
