using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SM64
{
    /// <summary>
    /// Third‑person camera that follows the player, orbits around it,
    /// and avoids clipping through geometry.
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

        private float _currentDistance;
        private float _yaw;
        private float _pitch = 15f;
        private SM64PlayerInput _input;

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
            if (target == null)
                return;

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
