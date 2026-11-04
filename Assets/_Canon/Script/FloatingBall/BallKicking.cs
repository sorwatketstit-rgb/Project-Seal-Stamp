using UnityEngine;

namespace SM64
{
    /// <summary>
    /// Reworked Ball Aiming & Shooting Controller.
    ///
    /// - Press and hold Shift:
    ///   - Camera transitions smoothly to a TPS over-the-shoulder spot (moves like a third-person shooter).
    ///   - A 3D Plane crosshair appears at the aim target (facing the camera, not a UI Canvas).
    ///   - Player cannot move and rotates to face the aim direction.
    ///   - Gravity is decreased (player falls much slower / floats if airborne).
    ///
    /// - Release Shift:
    ///   - The floating ball is shot in the aimed direction in all 3 axes (X, Y, Z).
    ///   - Camera smoothly returns to its original orbit follow position.
    ///   - Player movement and normal gravity are restored.
    /// </summary>
    public class BallKicking : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The ball's state controller. Auto-found in scene if empty.")]
        [SerializeField] private FloatingBallController ball;

        [Tooltip("Player controller. Auto-found on this GameObject if empty.")]
        [SerializeField] private SM64PlayerController playerController;

        [Tooltip("Camera controller. Auto-found on Camera.main if empty.")]
        [SerializeField] private SM64CameraController cameraController;

        [Tooltip("3D Plane Crosshair component. Auto-added if empty.")]
        [SerializeField] private CrosshairPlane crosshairPlane;

        [Tooltip("Transform used as fallback facing source.")]
        [SerializeField] private Transform facingSource;

        [Header("Aim & Shoot Settings")]
        [Tooltip("Layers that the aim raycast can target.")]
        [SerializeField] private LayerMask aimLayers = ~0;

        [Tooltip("Maximum distance to aim.")]
        [SerializeField] private float maxAimDistance = 60f;

        [Tooltip("Default distance if aiming into the sky or nothing is hit.")]
        [SerializeField] private float defaultAimDistance = 20f;

        [Header("Ball Origin Offset")]
        [Tooltip("Local-space offset applied to the ball's position when calculating the shoot direction.\n" +
                 "X = right/left, Y = up/down, Z = forward/back relative to the player's facing.\n" +
                 "Use this to shift the effective 'barrel' of the shot without moving the ball itself.")]
        [SerializeField] private Vector3 shootOriginOffset = Vector3.zero;

        [Header("Input")]
        [Tooltip("Key to hold for aiming and release to shoot.")]
        [SerializeField] private KeyCode kickKey = KeyCode.LeftShift;

        [Tooltip("Poll legacy input in addition to SM64PlayerInput.")]
        [SerializeField] private bool pollLegacyInput = true;

        public bool IsAiming { get; private set; }

        /// <summary>Backwards-compatible flag for player movement states.</summary>
        public bool IsRunning { get; set; }

        private SM64PlayerInput _input;

        private void Awake()
        {
            if (playerController == null) playerController = GetComponent<SM64PlayerController>();
            if (facingSource == null) facingSource = transform;

            if (crosshairPlane == null) crosshairPlane = GetComponent<CrosshairPlane>();
            if (crosshairPlane == null) crosshairPlane = gameObject.AddComponent<CrosshairPlane>();
        }

        private void Start()
        {
            FindReferencesIfNeeded();
        }

        private void FindReferencesIfNeeded()
        {
            if (ball == null) ball = Object.FindFirstObjectByType<FloatingBallController>();
            if (playerController == null) playerController = GetComponent<SM64PlayerController>();
            if (playerController != null && _input == null) _input = playerController.Input;
            if (_input == null) _input = GetComponent<SM64PlayerInput>();

            if (cameraController == null)
            {
                Camera cam = Camera.main;
                if (cam != null) cameraController = cam.GetComponent<SM64CameraController>();
            }
            if (cameraController == null)
            {
                cameraController = Object.FindFirstObjectByType<SM64CameraController>();
            }
        }

        private void Update()
        {
            FindReferencesIfNeeded();

            bool isShiftHeld = ReadShiftHeld();

            // 1. Enter Aiming: when Shift is held and not yet aiming
            if (isShiftHeld && !IsAiming)
            {
                // Can only enter aiming if the ball is available and not already kicked
                if (ball != null && !ball.IsKicked)
                {
                    StartAiming();
                }
            }

            // 2. While Aiming: update player facing, camera, crosshair
            if (IsAiming)
            {
                if (isShiftHeld)
                {
                    UpdateAiming();
                }
                else
                {
                    // 3. Release Shift: shoot ball in 3D direction!
                    ShootBallAndExitAiming();
                }
            }
        }

        private bool ReadShiftHeld()
        {
            if (_input != null)
            {
                return _input.KickHeld;
            }

            if (pollLegacyInput)
            {
                return Input.GetKey(kickKey) || Input.GetKey(KeyCode.RightShift);
            }

            return false;
        }

        private void StartAiming()
        {
            IsAiming = true;

            if (playerController != null)
            {
                playerController.IsAiming = true;
                playerController.HorizontalVelocity = Vector3.zero;
            }

            if (_input != null)
            {
                _input.IsAiming = true;
            }

            if (cameraController != null)
            {
                cameraController.SetAimMode(true);
            }

            if (crosshairPlane != null)
            {
                crosshairPlane.Show();
            }
        }

        private void UpdateAiming()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            // Update 3D Plane crosshair position & orientation first
            if (crosshairPlane != null)
            {
                crosshairPlane.UpdateAim(cam);
            }

            // Player cannot move horizontally
            if (playerController != null)
            {
                playerController.HorizontalVelocity = Vector3.zero;

                // Player character smoothly rotates to face the exact aim target point
                Vector3 targetPos = (crosshairPlane != null && crosshairPlane.CurrentAimPoint != Vector3.zero)
                    ? crosshairPlane.CurrentAimPoint
                    : (cameraController != null && cameraController.CurrentAimTargetPoint != Vector3.zero
                        ? cameraController.CurrentAimTargetPoint
                        : (cam.transform.position + cam.transform.forward * defaultAimDistance));

                Vector3 toTarget = targetPos - playerController.transform.position;
                toTarget.y = 0f;

                if (toTarget.sqrMagnitude > 0.001f)
                {
                    playerController.RotateTowards(toTarget.normalized, 0.05f);
                }
            }
        }

        private void ShootBallAndExitAiming()
        {
            IsAiming = false;

            Camera cam = Camera.main;
            Vector3 shootDirection = cam != null ? cam.transform.forward : transform.forward;

            // Calculate precise 3D target point from camera raycast
            if (cam != null)
            {
                Ray ray = new Ray(cam.transform.position, cam.transform.forward);
                Vector3 targetPoint;
                if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, aimLayers, QueryTriggerInteraction.Ignore))
                {
                    targetPoint = hit.point;
                }
                else
                {
                    targetPoint = ray.origin + ray.direction * defaultAimDistance;
                }

                // Vector from ball position (or player) to target in all 3 axes
                // Apply shootOriginOffset in the player's local space so X/Y/Z are intuitive
                Vector3 ballPos = ball != null ? ball.transform.position : transform.position;
                Quaternion playerRot = transform.rotation;
                Vector3 origin = ballPos + playerRot * shootOriginOffset;

                Vector3 dir = (targetPoint - origin);
                if (dir.sqrMagnitude > 0.001f)
                {
                    shootDirection = dir.normalized;
                }
            }

            // Shoot the ball in all 3 axes!
            if (ball != null)
            {
                ball.RequestKick(shootDirection, full3D: true);
            }

            // Restore camera, player, crosshair
            if (cameraController != null)
            {
                cameraController.SetAimMode(false);
            }

            if (crosshairPlane != null)
            {
                crosshairPlane.Hide();
            }

            if (playerController != null)
            {
                playerController.IsAiming = false;
            }

            if (_input != null)
            {
                _input.IsAiming = false;
            }
        }

        /// <summary>
        /// Backwards-compatible kick trigger.
        /// </summary>
        public bool TryKick()
        {
            if (ball == null || ball.IsKicked) return false;
            Camera cam = Camera.main;
            Vector3 dir = cam != null ? cam.transform.forward : transform.forward;
            return ball.RequestKick(dir, full3D: true);
        }

        private void OnDisable()
        {
            if (IsAiming)
            {
                IsAiming = false;
                if (cameraController != null) cameraController.SetAimMode(false);
                if (crosshairPlane != null) crosshairPlane.Hide();
                if (playerController != null) playerController.IsAiming = false;
                if (_input != null) _input.IsAiming = false;
            }
        }
    }
}
