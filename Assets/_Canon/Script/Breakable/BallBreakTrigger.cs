using UnityEngine;

namespace SM64
{
    /// <summary>
    /// Breaks BreakableObjects the ball touches while it is in the Kicked stage.
    ///
    /// Unlike the old version this does NOT rely on OnTriggerEnter (which needs a Rigidbody,
    /// a matching layer-collision matrix, and a collider that isn't tunnelling). Instead it
    /// sweeps a sphere along the ball's path every frame, so it works even if the ball is moved
    /// by plain transform code (FloatingBallLocation) and moves fast.
    ///
    /// Setup:
    ///   1. Add to the ball (next to FloatingBallController).
    ///   2. Set 'breakableLayers' to your "Breakable" layer (it falls back to a layer named "Breakable").
    ///   3. Breakable objects need a Collider (trigger or not, static is fine) + BreakableObject.
    ///   4. Turn on Gizmos in the Scene AND Game view to see the hit sphere.
    /// </summary>
    [RequireComponent(typeof(FloatingBallController))]
    public class BallBreakTrigger : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private FloatingBallController ballController;

        [Header("Detection")]
        [Tooltip("Layers that can be broken. If left at 'Nothing', falls back to a layer named 'Breakable'.")]
        [SerializeField] private LayerMask breakableLayers;

        [Tooltip("World-space radius of the hit sphere (NOT affected by the ball's scale).")]
        [SerializeField] private float hitRadius = 0.5f;

        [Tooltip("Local-space offset of the hit sphere's centre.")]
        [SerializeField] private Vector3 centerOffset = Vector3.zero;

        [Tooltip("If true, only breaks between onKickShoot and onKickReturned (requires the location script to call NotifyKickShot). " +
                 "If false, breaks during the entire Kicked stage.")]
        [SerializeField] private bool onlyDuringShootPhase = false;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = true;

        private readonly Collider[] _overlaps = new Collider[32];
        private readonly RaycastHit[] _sweepHits = new RaycastHit[32];

        private Vector3 _lastPos;
        private bool _hasLast;
        private bool _shootPhase;
        private int _mask;

        private Vector3 HitCenter => transform.TransformPoint(centerOffset);
        private bool IsActive => ballController != null && ballController.IsKicked && (!onlyDuringShootPhase || _shootPhase);

        private void Reset()
        {
            breakableLayers = LayerMask.GetMask("Breakable");
        }

        private void Awake()
        {
            if (ballController == null)
                ballController = GetComponent<FloatingBallController>();

            _mask = breakableLayers.value;
            if (_mask == 0)
            {
                _mask = LayerMask.GetMask("Breakable");
                Debug.LogWarning("[BallBreakTrigger] 'breakableLayers' was empty - falling back to layer 'Breakable'. " +
                                 (_mask == 0 ? "That layer doesn't exist either! Create it and assign it." : "Assign the mask in the Inspector."), this);
            }
        }

        private void OnEnable()
        {
            if (ballController == null)
                return;
            ballController.onKickShoot.AddListener(OnKickShoot);
            ballController.onKickReturned.AddListener(OnKickReturned);
        }

        private void OnDisable()
        {
            if (ballController == null)
                return;
            ballController.onKickShoot.RemoveListener(OnKickShoot);
            ballController.onKickReturned.RemoveListener(OnKickReturned);
        }

        private void OnKickShoot() => _shootPhase = true;
        private void OnKickReturned() => _shootPhase = false;

        // LateUpdate so we read the ball's final position after FloatingBallLocation moved it.
        private void LateUpdate()
        {
            Vector3 pos = HitCenter;

            if (!IsActive)
            {
                _hasLast = false;
                if (ballController != null && !ballController.IsKicked)
                    _shootPhase = false;
                return;
            }

            if (!_hasLast)
            {
                _lastPos = pos;
                _hasLast = true;
            }

            // 1. Sphere already overlapping something at the current position
            int count = Physics.OverlapSphereNonAlloc(pos, hitRadius, _overlaps, _mask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
                TryBreak(_overlaps[i]);

            // 2. Sweep along the path travelled this frame (catches fast movement / tunnelling)
            Vector3 delta = pos - _lastPos;
            float dist = delta.magnitude;
            if (dist > 0.001f)
            {
                int hits = Physics.SphereCastNonAlloc(_lastPos, hitRadius, delta / dist, _sweepHits, dist, _mask, QueryTriggerInteraction.Collide);
                for (int i = 0; i < hits; i++)
                    TryBreak(_sweepHits[i].collider);

                Debug.DrawLine(_lastPos, pos, Color.red, 1f);
            }

            _lastPos = pos;
        }

        private void TryBreak(Collider other)
        {
            if (other == null)
                return;

            BreakableObject breakable = other.GetComponent<BreakableObject>();
            if (breakable == null)
                breakable = other.GetComponentInParent<BreakableObject>();

            if (breakable == null)
            {
                if (debugLogs)
                    Debug.LogWarning($"[BallBreakTrigger] Hit '{other.name}' on a breakable layer but it has no BreakableObject (on it or a parent).", other);
                return;
            }

            if (breakable.IsBroken)
                return;

            if (debugLogs)
                Debug.Log($"[BallBreakTrigger] Breaking '{breakable.name}'", breakable);
            breakable.Break();
        }

#if UNITY_EDITOR
        // OnDrawGizmos (not ...Selected) so it's always visible when Gizmos are enabled.
        private void OnDrawGizmos()
        {
            bool active = Application.isPlaying && IsActive;
            Gizmos.color = active ? new Color(1f, 0.2f, 0f, 0.9f) : new Color(1f, 0.9f, 0f, 0.6f);
            Gizmos.DrawWireSphere(HitCenter, hitRadius);

            if (active)
            {
                Gizmos.color = new Color(1f, 0.2f, 0f, 0.15f);
                Gizmos.DrawSphere(HitCenter, hitRadius);
            }
        }
#endif
    }
}
