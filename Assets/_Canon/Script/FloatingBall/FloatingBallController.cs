using UnityEngine;
using UnityEngine.Events;

namespace SM64
{
    /// <summary>
    /// Current operational stage of the floating ball.
    /// </summary>
    public enum BallStage
    {
        HasPlayer,
        Moving
    }

    /// <summary>
    /// Floating companion ball that hovers around the player.
    /// - Stage 'HasPlayer': Ball stays stationary (or hovers in place) as long as the player is inside its spherical range.
    /// - When the player exits the spherical range, state changes to 'Moving'.
    /// - Stage 'Moving': Ball flies in 3D (all 3 axes) toward the player until it reaches 'stopDistance' (default 5 units),
    ///   at which point it switches back to 'HasPlayer'.
    /// </summary>
    public class FloatingBallController : MonoBehaviour
    {
        [Header("Targeting")]
        [Tooltip("Tag used to find the player GameObject if no target is assigned.")]
        [SerializeField] private string playerTag = "Player";

        [Tooltip("Direct reference to player transform. If left empty, will search by tag.")]
        [SerializeField] private Transform playerTarget;

        [Tooltip("Offset from the player's pivot (e.g. chest/center height).")]
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.2f, 0f);

        [Header("Range & Distances")]
        [Tooltip("Spherical detection range radius. If player exits this radius while in HasPlayer stage, ball starts moving.")]
        [SerializeField] private float detectionRadius = 10f;

        [Tooltip("Distance from player at which the ball stops moving and switches back to HasPlayer stage.")]
        [SerializeField] private float stopDistance = 5f;

        [Header("Flight Movement")]
        [Tooltip("Maximum flight speed in units per second.")]
        [SerializeField] private float moveSpeed = 7f;

        [Tooltip("Acceleration / smoothing time for smooth flight damping.")]
        [SerializeField] private float smoothTime = 0.35f;

        [Tooltip("If true, uses smooth damping. If false, uses constant linear speed.")]
        [SerializeField] private bool useSmoothMovement = true;

        [Header("Idle Hover / Floating VFX")]
        [Tooltip("Enable subtle floating / bobbing motion when hovering.")]
        [SerializeField] private bool enableHoverBobbing = true;

        [Tooltip("Hover bobbing vertical amplitude.")]
        [SerializeField] private float hoverAmplitude = 0.2f;

        [Tooltip("Hover bobbing frequency (cycles per second).")]
        [SerializeField] private float hoverFrequency = 1.5f;

        [Header("Debug & State")]
        [SerializeField] private BallStage currentStage = BallStage.HasPlayer;

        [Header("Events")]
        public UnityEvent<BallStage> onStageChanged;
        public UnityEvent onStartMoving;
        public UnityEvent onArrivedAtPlayer;

        // Runtime variables
        private Vector3 _currentVelocity;
        private Vector3 _basePosition;
        private float _hoverTimer;

        /// <summary>
        /// Current state/stage of the floating ball.
        /// </summary>
        public BallStage CurrentStage => currentStage;

        /// <summary>
        /// Target transform being followed.
        /// </summary>
        public Transform PlayerTarget
        {
            get => playerTarget;
            set => playerTarget = value;
        }

        /// <summary>
        /// Detection radius around the ball.
        /// </summary>
        public float DetectionRadius
        {
            get => detectionRadius;
            set => detectionRadius = Mathf.Max(0.1f, value);
        }

        /// <summary>
        /// Stopping distance to player.
        /// </summary>
        public float StopDistance
        {
            get => stopDistance;
            set => stopDistance = Mathf.Max(0f, value);
        }

        private void Awake()
        {
            _basePosition = transform.position;
            _hoverTimer = Random.Range(0f, 2f * Mathf.PI);
        }

        private void Start()
        {
            FindPlayerIfNeeded();
            // Validate stop distance is not larger than detection radius for expected behavior
            if (stopDistance > detectionRadius)
            {
                Debug.LogWarning($"[FloatingBallController] Stop distance ({stopDistance}) is greater than detection radius ({detectionRadius}). Clamping stop distance to detection radius.", this);
                stopDistance = detectionRadius;
            }
        }

        private void Update()
        {
            FindPlayerIfNeeded();

            if (playerTarget == null)
            {
                return;
            }

            Vector3 destination = playerTarget.position + targetOffset;
            float distanceToPlayer = Vector3.Distance(_basePosition, destination);

            switch (currentStage)
            {
                case BallStage.HasPlayer:
                    HandleHasPlayerStage(distanceToPlayer);
                    break;

                case BallStage.Moving:
                    HandleMovingStage(destination, distanceToPlayer);
                    break;
            }

            ApplyHoverBobbing();
        }

        /// <summary>
        /// Logic during HasPlayer stage: remains stationary until player exits spherical range.
        /// </summary>
        private void HandleHasPlayerStage(float distanceToPlayer)
        {
            // If the player leaves the spherical detection range, switch to Moving stage
            if (distanceToPlayer > detectionRadius)
            {
                SetStage(BallStage.Moving);
            }
        }

        /// <summary>
        /// Logic during Moving stage: flies in 3D toward player until distance reaches stopDistance.
        /// </summary>
        private void HandleMovingStage(Vector3 destination, float distanceToPlayer)
        {
            // Fly toward destination in 3D (X, Y, Z axes)
            if (useSmoothMovement)
            {
                _basePosition = Vector3.SmoothDamp(_basePosition, destination, ref _currentVelocity, smoothTime, moveSpeed);
            }
            else
            {
                _basePosition = Vector3.MoveTowards(_basePosition, destination, moveSpeed * Time.deltaTime);
            }

            // Recalculate distance after movement
            float currentDist = Vector3.Distance(_basePosition, destination);

            // Once within stop distance (e.g. 5 units), return to HasPlayer stage
            if (currentDist <= stopDistance)
            {
                SetStage(BallStage.HasPlayer);
            }
        }

        /// <summary>
        /// Applies optional subtle sinusoidal hover bobbing on top of the base position.
        /// </summary>
        private void ApplyHoverBobbing()
        {
            if (enableHoverBobbing)
            {
                _hoverTimer += Time.deltaTime * hoverFrequency * Mathf.PI * 2f;
                float yOffset = Mathf.Sin(_hoverTimer) * hoverAmplitude;
                transform.position = _basePosition + new Vector3(0f, yOffset, 0f);
            }
            else
            {
                transform.position = _basePosition;
            }
        }

        /// <summary>
        /// Changes state and triggers associated events.
        /// </summary>
        public void SetStage(BallStage newStage)
        {
            if (currentStage == newStage) return;

            currentStage = newStage;
            onStageChanged?.Invoke(currentStage);

            if (currentStage == BallStage.Moving)
            {
                onStartMoving?.Invoke();
            }
            else if (currentStage == BallStage.HasPlayer)
            {
                _currentVelocity = Vector3.zero;
                onArrivedAtPlayer?.Invoke();
            }
        }

        /// <summary>
        /// Tries to find the GameObject with the specified playerTag if target is missing.
        /// </summary>
        private void FindPlayerIfNeeded()
        {
            if (playerTarget != null && playerTarget.gameObject.activeInHierarchy)
            {
                return;
            }

            if (!string.IsNullOrEmpty(playerTag))
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
                if (playerObj != null)
                {
                    playerTarget = playerObj.transform;
                }
            }
        }

        /// <summary>
        /// Synchronizes the internal base position if the transform was moved externally.
        /// </summary>
        public void SetPosition(Vector3 newPosition)
        {
            _basePosition = newPosition;
            transform.position = newPosition;
            _currentVelocity = Vector3.zero;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector3 center = Application.isPlaying ? _basePosition : transform.position;

            // Draw Detection Radius Sphere (Spherical range)
            Gizmos.color = (currentStage == BallStage.HasPlayer) ? new Color(0.2f, 1f, 0.4f, 0.25f) : new Color(1f, 0.8f, 0.2f, 0.25f);
            Gizmos.DrawWireSphere(center, detectionRadius);

            // Draw Stop Distance Sphere
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.35f);
            Gizmos.DrawWireSphere(center, stopDistance);

            // Draw flight path line to target
            if (playerTarget != null)
            {
                Vector3 dest = playerTarget.position + targetOffset;
                Gizmos.color = (currentStage == BallStage.Moving) ? Color.yellow : Color.gray;
                Gizmos.DrawLine(center, dest);
            }
        }
#endif
    }
}
