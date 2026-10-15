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
        Moving,
        /// <summary>
        /// Ball is executing the kick sequence (wind-up → projectile shot → arc return).
        /// </summary>
        Kicked
    }

    /// <summary>
    /// State manager for the floating ball. It decides WHICH stage the ball is in and raises events,
    /// but it never moves the ball — all positional control lives in <see cref="FloatingBallLocation"/>.
    /// - HasPlayer: ball is idle. If the player leaves 'detectionRadius', switches to Moving.
    /// - Moving:    ball is following the player. Once within 'stopDistance', switches back to HasPlayer.
    /// - Kicked:    entered via <see cref="RequestKick"/>; left when the location script reports the kick finished.
    /// </summary>
    public class FloatingBallController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Script that owns the ball's position. Found on this GameObject if left empty.")]
        [SerializeField] private FloatingBallLocation location;

        [Header("Range & Distances")]
        [Tooltip("If the player is further than this from the ball while in HasPlayer, the ball starts moving.")]
        [SerializeField] private float detectionRadius = 10f;

        [Tooltip("Distance from the player at which the ball stops moving and switches back to HasPlayer.")]
        [SerializeField] private float stopDistance = 5f;

        [Header("Debug & State")]
        [SerializeField] private BallStage currentStage = BallStage.HasPlayer;

        [Header("Collection")]
        [Tooltip("Layer mask for collectable items like letters.")]
        [SerializeField] private LayerMask collectableLayers;

        [Header("Events")]
        public UnityEvent<BallStage> onStageChanged;
        public UnityEvent onStartMoving;
        public UnityEvent onArrivedAtPlayer;
        public UnityEvent onKicked;       // kick sequence requested/started
        public UnityEvent onKickShoot;    // ball launched from the wind-up point
        public UnityEvent onKickReturned; // ball is back at its origin

        public BallStage CurrentStage => currentStage;
        public bool IsKicked => currentStage == BallStage.Kicked;

        /// <summary>Horizontal facing direction supplied with the most recent kick request.</summary>
        public Vector3 KickForward { get; private set; } = Vector3.forward;

        public float DetectionRadius
        {
            get => detectionRadius;
            set => detectionRadius = Mathf.Max(0.1f, value);
        }

        public float StopDistance
        {
            get => stopDistance;
            set => stopDistance = Mathf.Max(0f, value);
        }

        private void Awake()
        {
            if (location == null) location = GetComponent<FloatingBallLocation>();
        }

        private void Start()
        {
            if (stopDistance > detectionRadius)
            {
                Debug.LogWarning($"[FloatingBallController] Stop distance ({stopDistance}) is greater than detection radius ({detectionRadius}). Clamping stop distance to detection radius.", this);
                stopDistance = detectionRadius;
            }
        }

        private void Update()
        {
            if (location == null || !location.HasTarget) return;

            float distance = location.DistanceToTarget;

            switch (currentStage)
            {
                case BallStage.HasPlayer:
                    if (distance > detectionRadius) SetStage(BallStage.Moving);
                    break;

                case BallStage.Moving:
                    if (distance <= stopDistance) SetStage(BallStage.HasPlayer);
                    break;

                case BallStage.Kicked:
                    // Stage is left only via NotifyKickFinished().
                    break;
            }
        }

        /// <summary>Changes state and triggers associated events.</summary>
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
                onArrivedAtPlayer?.Invoke();
            }
        }

        /// <summary>
        /// Asks the ball to perform a kick. Returns false if it is already kicking.
        /// Called by <see cref="BallKicking"/>. The actual movement is carried out by <see cref="FloatingBallLocation"/>.
        /// </summary>
        public bool RequestKick(Vector3 playerForward)
        {
            if (currentStage == BallStage.Kicked) return false;

            playerForward.y = 0f;
            KickForward = playerForward.sqrMagnitude < 0.001f ? Vector3.forward : playerForward.normalized;

            onKicked?.Invoke();

            // Enter Kicked from any stage
            currentStage = BallStage.Kicked;
            onStageChanged?.Invoke(currentStage);
            return true;
        }

        /// <summary>
        /// Backwards-compatible alias for <see cref="RequestKick"/> so existing callers
        /// (e.g. PlayerGroundedState) keep working.
        /// </summary>
        public void Kick(Vector3 playerForward)
        {
            RequestKick(playerForward);
        }

        /// <summary>Called by the location script when the ball launches from the wind-up point.</summary>
        public void NotifyKickShot()
        {
            if (currentStage == BallStage.Kicked) onKickShoot?.Invoke();
        }

        /// <summary>Called by the location script once the ball is back at its origin.</summary>
        public void NotifyKickFinished()
        {
            if (currentStage != BallStage.Kicked) return;

            onKickReturned?.Invoke();
            SetStage(BallStage.HasPlayer);
        }

        private void OnTriggerEnter(Collider other)
        {
            // Check if the collided object matches the collectable layer mask or tag
            if (((1 << other.gameObject.layer) & collectableLayers) != 0 || other.CompareTag("Letter"))
            {
                MailLetterCollectable letter = other.GetComponent<MailLetterCollectable>();
                if (letter != null && letter.CanBeCollected)
                {
                    letter.Collect();

                    if (CollectableManager.Instance != null)
                    {
                        CollectableManager.Instance.AddLetters(1);
                    }
                    else
                    {
                        Debug.LogWarning("[FloatingBallController] CollectableManager.Instance is null!");
                    }
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector3 center = (Application.isPlaying && location != null) ? location.BasePosition : transform.position;

            Gizmos.color = (currentStage == BallStage.HasPlayer) ? new Color(0.2f, 1f, 0.4f, 0.25f) : new Color(1f, 0.8f, 0.2f, 0.25f);
            Gizmos.DrawWireSphere(center, detectionRadius);

            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.35f);
            Gizmos.DrawWireSphere(center, stopDistance);
        }
#endif
    }
}