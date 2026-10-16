using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SM64
{
    /// <summary>
    /// Genshin Impact-style Menu Controller.
    /// - Toggled with ESC key or UI button.
    /// - Does NOT pause the game (Time.timeScale stays 1.0f).
    /// - Disables player actions while allowing menu navigation with mouse cursor.
    /// - Draws 4 candidate dots around the player, removes any that collide with obstacles,
    ///   and smoothly moves the camera to one of the remaining valid dots (swapping position each time).
    /// - Enables/disables a Menu UI object.
    /// </summary>
    public class GenshinMenuManager : MonoBehaviour
    {
        public static GenshinMenuManager Instance { get; private set; }

        [Header("Menu UI Object")]
        [Tooltip("The Menu UI GameObject enabled when menu is open.")]
        [SerializeField] private GameObject menuUIObject;
        [Tooltip("The GameObject disabled when menu is open.")]
        [SerializeField] private GameObject menuCloseObject;

        [Header("Scene References (Auto-discovered if unassigned)")]
        [SerializeField] private SM64PlayerController playerController;
        [SerializeField] private SM64PlayerInput playerInput;
        [SerializeField] private SM64CameraController cameraController;

        [Header("4-Dot Pool Settings")]
        [Tooltip("Candidate direction angles relative to player forward facing (in degrees). Default 4 diagonal angles frame the character nicely.")]
        [SerializeField] private float[] dotAngles = new float[] { 45f, -45f, 135f, -135f };

        [Tooltip("Distance from the player to each candidate camera dot.")]
        [SerializeField] private float dotDistance = 2.8f;

        [Tooltip("Height offset of candidate camera dots above the player's position.")]
        [SerializeField] private float dotHeight = 1.35f;

        [Tooltip("Sphere collision radius used to detect if a dot intersects an obstacle.")]
        [SerializeField] private float collisionCheckRadius = 0.25f;

        [Tooltip("Layer mask representing collidable objects (walls, environment, obstacles).")]
        [SerializeField] private LayerMask obstructionMask = ~0;

        [Header("Transition Settings")]
        [Tooltip("Time in seconds for camera to travel to/from the menu position.")]
        [SerializeField] private float transitionDuration = 0.45f;

        [Header("Debug & Gizmos")]
        [Tooltip("Draw visual gizmos showing the 4 dots and their collision statuses in the Scene view.")]
        [SerializeField] private bool showGizmos = true;

        [SerializeField] private bool isMenuOpen = false;
        public bool IsMenuOpen => isMenuOpen;

        // Candidate dot structure for pool evaluation
        public struct DotCandidate
        {
            public int index;
            public Vector3 position;
            public bool isClear;
            public float angle;
        }

        private int _lastChosenDotIndex = -1;
        private List<DotCandidate> _latestEvaluatedDots = new List<DotCandidate>();
        private Vector3 _currentChosenDotPosition;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Auto-resolve references if not wired in inspector
            if (playerController == null)
            {
                playerController = FindFirstObjectByType<SM64PlayerController>();
            }

            if (playerInput == null && playerController != null)
            {
                playerInput = playerController.GetComponent<SM64PlayerInput>();
            }
            if (playerInput == null)
            {
                playerInput = FindFirstObjectByType<SM64PlayerInput>();
            }

            if (cameraController == null)
            {
                cameraController = FindFirstObjectByType<SM64CameraController>();
            }

            // Ensure Menu UI starts disabled
            if (menuUIObject != null)
            {
                menuUIObject.SetActive(false);
            }
        }

        private void Update()
        {
            CheckMenuInput();
        }

        private void CheckMenuInput()
        {
            bool escPressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                escPressed = true;
            }
#else
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                escPressed = true;
            }
#endif

            if (escPressed)
            {
                ToggleMenu();
            }
        }

        /// <summary>
        /// Toggles the menu on or off. Can be linked to UI buttons or hotkeys.
        /// </summary>
        public void ToggleMenu()
        {
            if (isMenuOpen)
            {
                CloseMenu();
            }
            else
            {
                OpenMenu();
            }
        }

        /// <summary>
        /// Opens the menu:
        /// 1. Keeps Time.timeScale = 1 (no game pause).
        /// 2. Disables player actions and unlocks mouse cursor.
        /// 3. Evaluates 4 candidate dots around player, filters out obstructed dots.
        /// 4. Picks a new position from the pool (swapping each time).
        /// 5. Transitions camera to the chosen dot.
        /// 6. Enables the menu UI object.
        /// </summary>
        public void OpenMenu()
        {
            if (isMenuOpen) return;
            isMenuOpen = true;

            // Block player gameplay inputs and free cursor for UI navigation
            if (playerInput != null)
            {
                playerInput.IsInputBlocked = true;
            }

            if (playerController != null)
            {
                // Smoothly stop ongoing horizontal momentum so player rests in idle
                playerController.HorizontalVelocity = Vector3.zero;
            }

            // Enable Menu UI object
            if (menuUIObject != null)
            {
                menuUIObject.SetActive(true);
                menuCloseObject.SetActive(false);
            }

            // Evaluate candidate dots and choose camera position
            Vector3 targetPivot = GetPlayerTargetPivot();
            Vector3 chosenMenuPosition = ChooseCameraDotPosition(targetPivot);
            _currentChosenDotPosition = chosenMenuPosition;

            // Transition camera
            if (cameraController != null)
            {
                cameraController.StartMenuCamera(chosenMenuPosition, targetPivot, transitionDuration);
            }
        }

        /// <summary>
        /// Closes the menu:
        /// 1. Disables the menu UI object.
        /// 2. Transitions camera back to its saved gameplay position.
        /// 3. Restores player action inputs.
        /// </summary>
        public void CloseMenu()
        {
            if (!isMenuOpen) return;
            isMenuOpen = false;

            // Disable Menu UI object
            if (menuUIObject != null)
            {
                menuUIObject.SetActive(false);
                menuCloseObject.SetActive(true);
            }

            // Transition camera back to saved gameplay position
            if (cameraController != null)
            {
                cameraController.EndMenuCamera(transitionDuration);
            }

            // Restore player gameplay inputs
            if (playerInput != null)
            {
                playerInput.IsInputBlocked = false;
            }
        }

        /// <summary>
        /// Evaluates 4 dots around player in 4 directions.
        /// If a dot hits any collidable object or has blocked line of sight, it is marked invalid (removed from pool).
        /// </summary>
        public List<DotCandidate> EvaluateDots(Vector3 pivot)
        {
            List<DotCandidate> candidates = new List<DotCandidate>();
            if (playerController == null) return candidates;

            Transform playerT = playerController.transform;
            Vector3 playerPos = playerT.position;
            Vector3 forward = playerT.forward;

            for (int i = 0; i < dotAngles.Length; i++)
            {
                float angle = dotAngles[i];
                Quaternion rot = Quaternion.Euler(0f, angle, 0f);
                Vector3 direction = rot * forward;

                Vector3 dotPosition = playerPos + direction.normalized * dotDistance + Vector3.up * dotHeight;

                // 1. Check if dot position intersects any collidable object
                bool hit = Physics.CheckSphere(dotPosition, collisionCheckRadius, obstructionMask, QueryTriggerInteraction.Ignore);

                // 2. Check if line of sight from player pivot to dot is obstructed
                if (!hit)
                {
                    Vector3 rayDir = dotPosition - pivot;
                    float rayDist = rayDir.magnitude;
                    if (rayDist > 0.05f)
                    {
                        if (Physics.SphereCast(pivot, collisionCheckRadius, rayDir.normalized, out RaycastHit hitInfo, rayDist, obstructionMask, QueryTriggerInteraction.Ignore))
                        {
                            hit = true;
                        }
                    }
                }

                candidates.Add(new DotCandidate
                {
                    index = i,
                    position = dotPosition,
                    isClear = !hit,
                    angle = angle
                });
            }

            return candidates;
        }

        /// <summary>
        /// Chooses 1 dot from the valid pool, ensuring it swaps between different positions each time.
        /// </summary>
        private Vector3 ChooseCameraDotPosition(Vector3 pivot)
        {
            _latestEvaluatedDots = EvaluateDots(pivot);

            // Filter pool of clear/non-colliding dots
            List<DotCandidate> pool = _latestEvaluatedDots.Where(d => d.isClear).ToList();

            if (pool.Count > 0)
            {
                // Prefer options other than the last chosen dot so the camera swaps position each time
                List<DotCandidate> swapCandidates = pool.Where(d => d.index != _lastChosenDotIndex).ToList();
                DotCandidate chosen = swapCandidates.Count > 0
                    ? swapCandidates[Random.Range(0, swapCandidates.Count)]
                    : pool[0];

                _lastChosenDotIndex = chosen.index;
                return chosen.position;
            }

            // Fallback: if all 4 dots hit collidable objects (e.g. extremely confined space),
            // safely place camera in front of player slightly closer.
            if (playerController != null)
            {
                Vector3 fallbackDir = playerController.transform.forward;
                Vector3 fallbackPos = playerController.transform.position + fallbackDir * (dotDistance * 0.6f) + Vector3.up * dotHeight;
                _lastChosenDotIndex = -1;
                return fallbackPos;
            }

            return pivot + Vector3.forward * 2f;
        }

        private Vector3 GetPlayerTargetPivot()
        {
            if (cameraController != null && cameraController.target != null)
            {
                return cameraController.target.position + cameraController.targetOffset;
            }

            if (playerController != null)
            {
                return playerController.transform.position + Vector3.up * 1.5f;
            }

            return transform.position;
        }

        private void OnDrawGizmosSelected()
        {
            if (!showGizmos) return;

            Vector3 pivot = GetPlayerTargetPivot();
            List<DotCandidate> candidates = Application.isPlaying && _latestEvaluatedDots.Count > 0
                ? _latestEvaluatedDots
                : EvaluateDots(pivot);

            for (int i = 0; i < candidates.Count; i++)
            {
                DotCandidate dot = candidates[i];

                if (isMenuOpen && dot.position == _currentChosenDotPosition)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireSphere(dot.position, collisionCheckRadius * 1.5f);
                }
                else if (dot.isClear)
                {
                    Gizmos.color = Color.green;
                }
                else
                {
                    Gizmos.color = Color.red;
                }

                Gizmos.DrawSphere(dot.position, collisionCheckRadius);
                Gizmos.DrawLine(pivot, dot.position);
            }
        }
    }
}
