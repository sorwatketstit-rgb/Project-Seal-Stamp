using UnityEngine;

namespace SM64
{
    /// <summary>
    /// Attach this to the floating ball (or a child trigger collider on it).
    ///
    /// It owns a **kinematic Trigger Collider** that fires only while the ball is in the
    /// <see cref="BallStage.Kicked"/> → Shoot phase. When the trigger overlaps a Collider
    /// whose GameObject is on the <c>Breakable</c> layer it calls
    /// <see cref="BreakableObject.Break"/> on it.
    ///
    /// Setup checklist:
    ///   1. Add this component to the ball GameObject (same as FloatingBallController).
    ///   2. Assign 'ballController' (auto-found on the same GO if left empty).
    ///   3. Create a "Breakable" layer in Project Settings → Physics.
    ///   4. Set the ball's collision matrix so the ball layer overlaps "Breakable".
    ///   5. The trigger collider is created automatically; set its size via 'triggerRadius'.
    /// </summary>
    [RequireComponent(typeof(FloatingBallController))]
    public class BallBreakTrigger : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The ball's stage controller. Auto-found on this GameObject if left empty.")]
        [SerializeField] private FloatingBallController ballController;

        [Header("Breakable Layer")]
        [Tooltip("Layer mask for objects that can be broken by the ball.")]
        [SerializeField] private LayerMask breakableLayers;

        [Header("Trigger Collider")]
        [Tooltip("Radius of the sphere trigger used to detect breakables. Should match the ball's visual radius.")]
        [SerializeField] private float triggerRadius = 0.4f;

        // The collider we manage — created in Awake if none is found.
        private SphereCollider _triggerCollider;

        // ─────────────────────── Unity lifecycle ────────────────────────

        private void Awake()
        {
            if (ballController == null)
                ballController = GetComponent<FloatingBallController>();

            EnsureTriggerCollider();
        }

        private void OnEnable()  => _triggerCollider.enabled = false; // start disabled; only active during Shoot
        private void OnDisable() => _triggerCollider.enabled = false;

        private void Update()
        {
            // Enable the trigger ONLY while the ball is in the Kicked (Shoot) phase.
            // FloatingBallController.IsKicked is true for the entire Kicked stage;
            // we can't easily query KickPhase from outside FloatingBallLocation,
            // so we activate during the full Kicked stage — it's close enough and
            // the actual detection only matters while the ball is flying anyway.
            bool shouldBeActive = ballController != null && ballController.IsKicked;

            if (_triggerCollider.enabled != shouldBeActive)
                _triggerCollider.enabled = shouldBeActive;
        }

        // ─────────────────────── Trigger detection ────────────────────────

        private void OnTriggerEnter(Collider other)
        {
            // Only act when we are in the Kicked stage
            if (ballController == null || !ballController.IsKicked) return;

            // Check if the other collider is on a Breakable layer
            if (!IsOnBreakableLayer(other.gameObject)) return;

            // Try to find and break the BreakableObject component
            BreakableObject breakable = other.GetComponent<BreakableObject>();
            if (breakable == null)
                breakable = other.GetComponentInParent<BreakableObject>();

            if (breakable != null)
            {
                breakable.Break();
            }
            else
            {
                Debug.LogWarning(
                    $"[BallBreakTrigger] Hit '{other.gameObject.name}' on a Breakable layer but no " +
                    $"BreakableObject component found. Add one to the object or its parent.", other.gameObject);
            }
        }

        // ─────────────────────── Helpers ────────────────────────

        private bool IsOnBreakableLayer(GameObject go)
        {
            return ((1 << go.layer) & breakableLayers.value) != 0;
        }

        /// <summary>
        /// Finds or creates the sphere trigger collider.
        /// We look for an existing SphereCollider marked as isTrigger first so designers can
        /// configure one manually; if none exists we create one at runtime.
        /// </summary>
        private void EnsureTriggerCollider()
        {
            // Look for any existing trigger sphere on this GO (not in children, to avoid conflicts)
            foreach (SphereCollider sc in GetComponents<SphereCollider>())
            {
                if (sc.isTrigger)
                {
                    _triggerCollider = sc;
                    _triggerCollider.radius = triggerRadius;
                    return;
                }
            }

            // None found — create one
            _triggerCollider = gameObject.AddComponent<SphereCollider>();
            _triggerCollider.isTrigger = true;
            _triggerCollider.radius = triggerRadius;
            _triggerCollider.center = Vector3.zero;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            bool isKicking = Application.isPlaying && ballController != null && ballController.IsKicked;
            Gizmos.color = isKicking
                ? new Color(1f, 0.3f, 0f, 0.5f)   // orange when active
                : new Color(1f, 0.8f, 0f, 0.15f); // faded yellow when idle
            Gizmos.DrawWireSphere(transform.position, triggerRadius);
        }
#endif
    }
}

