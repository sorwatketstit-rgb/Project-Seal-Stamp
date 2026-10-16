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
        }

        private void Start()
        {
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

            // Delta values are already frame-rate independent from Mouse.delta
            _yaw += look.x * rotateSpeed;
            _pitch -= look.y * rotateSpeed;
            _pitch = Mathf.Clamp(_pitch, -30f, 60f);

            // Handle Zoom Input
            HandleZoom();

            // Calculate pivot point and desired camera position
            Vector3 pivot = target.position + targetOffset;
            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 desiredPosition = pivot + rotation * new Vector3(0, 0, -_currentDistance);

            // SphereCast from pivot to desired position to prevent wall clipping
            Vector3 rayDirection = desiredPosition - pivot;
            float rayLength = rayDirection.magnitude;

            if (rayLength > 0.001f && Physics.SphereCast(pivot, cameraRadius, rayDirection.normalized, out RaycastHit hit, rayLength, obstructionMask, QueryTriggerInteraction.Ignore))
            {
                desiredPosition = pivot + rayDirection.normalized * Mathf.Max(hit.distance - cameraRadius, 0.1f);
            }

            // Apply transformations
            transform.position = desiredPosition;
            transform.LookAt(pivot);
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
