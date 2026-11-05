using UnityEngine;
using UnityEngine.Events;

namespace SM64
{
    /// <summary>
    /// Attach this to any GameObject on the "Breakable" layer.
    /// When Break() is called (e.g. by BallBreakTrigger) it:
    ///   1. Spawns/plays the break particle effect (detached, so it survives this object).
    ///   2. Reveals the hidden content (detached, so it survives this object).
    ///   3. Hides this object's visuals + colliders immediately.
    ///   4. Destroys / disables this object after 'disposalDelay'.
    ///   5. Fires UnityEvents.
    ///
    /// 'hiddenContent' and 'breakParticlePrefab' may be either prefab assets OR scene objects
    /// (including children of this object, which are automatically unparented first).
    /// </summary>
    public class BreakableObject : MonoBehaviour
    {
        [Header("Hidden Contents")]
        [Tooltip("Revealed on break. Can be a scene object (child or not) or a prefab asset (it gets instantiated).")]
        [SerializeField] private GameObject hiddenContent;

        [Tooltip("Apply the launch velocity to the hidden content's Rigidbody (if it has one).")]
        [SerializeField] private bool popOutContent = true;

        [Tooltip("Launch velocity applied to the hidden content's Rigidbody (if any) when revealed.")]
        [SerializeField] private Vector3 contentLaunchVelocity = new Vector3(0f, 5f, 0f);

        [Header("Break VFX")]
        [Tooltip("Particle effect (prefab asset or scene object). Played at this object's position on break.")]
        [SerializeField] private GameObject breakParticlePrefab;

        [Tooltip("Seconds before the spawned particle effect is destroyed. 0 = never.")]
        [SerializeField] private float particleLifetime = 3f;

        [Header("Destroy / Disable")]
        [SerializeField] private BreakDisposal disposal = BreakDisposal.Destroy;

        [Tooltip("Seconds before this object is destroyed / disabled. Visuals + colliders are hidden immediately.")]
        [SerializeField] private float disposalDelay = 0.5f;

        [Tooltip("Immediately disable this object's renderers and colliders when broken.")]
        [SerializeField] private bool hideVisualsOnBreak = true;

        [Header("State")]
        [SerializeField] private bool isBroken = false;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = true;

        [Header("Events")]
        public UnityEvent onBreak;

        public bool IsBroken => isBroken;

        public void Break()
        {
            if (isBroken)
                return;
            isBroken = true;

            // Order matters: detach/spawn everything we want to keep BEFORE hiding or destroying this object.
            SpawnBreakParticle();
            RevealContent();

            if (hideVisualsOnBreak)
                HideSelf();

            onBreak?.Invoke();
            DisposeThis();
        }

        // ─────────────────────── Internals ────────────────────────

        /// <summary>
        /// Returns a live scene instance of 'source': scene objects are detached from their parent
        /// and reused, prefab assets are instantiated. The result is always active and unparented.
        /// </summary>
        private GameObject GetLiveInstance(GameObject source, Vector3 position, Quaternion rotation, bool keepSceneObjectPosition)
        {
            GameObject instance;

            if (source.scene.IsValid())
            {
                instance = source;
                instance.transform.SetParent(null, true); // keep world position/scale
                if (!keepSceneObjectPosition)
                    instance.transform.SetPositionAndRotation(position, rotation);
            }
            else
            {
                instance = Instantiate(source, position, rotation);
            }

            instance.SetActive(true);
            return instance;
        }

        private void SpawnBreakParticle()
        {
            if (breakParticlePrefab == null)
            {
                if (debugLogs)
                    Debug.LogWarning("[BreakableObject] No break particle assigned.", this);
                return;
            }

            GameObject vfx = GetLiveInstance(breakParticlePrefab, transform.position, transform.rotation, false);

            // Make sure every particle system actually plays (covers Play On Awake being off).
            foreach (ParticleSystem ps in vfx.GetComponentsInChildren<ParticleSystem>(true))
            {
                ps.gameObject.SetActive(true);
                ps.Clear(true);
                ps.Play(true);
            }

            if (particleLifetime > 0f)
                Destroy(vfx, particleLifetime);
        }

        private void RevealContent()
        {
            if (hiddenContent == null)
            {
                if (debugLogs)
                    Debug.LogWarning("[BreakableObject] No hidden content assigned.", this);
                return;
            }

            // Scene object: stays where it was placed. Prefab asset: spawned at this object's position.
            GameObject content = GetLiveInstance(hiddenContent, transform.position, Quaternion.identity, true);

            if (popOutContent && content.TryGetComponent(out Rigidbody rb) && !rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.AddForce(contentLaunchVelocity, ForceMode.VelocityChange);
            }
        }

        private void HideSelf()
        {
            foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
                r.enabled = false;

            foreach (Collider c in GetComponentsInChildren<Collider>(true))
                c.enabled = false;
        }

        private void DisposeThis()
        {
            switch (disposal)
            {
                case BreakDisposal.Destroy:
                    Destroy(gameObject, disposalDelay);
                    break;

                case BreakDisposal.Disable:
                    if (disposalDelay > 0f)
                        Invoke(nameof(DisableSelf), disposalDelay);
                    else
                        gameObject.SetActive(false);
                    break;

                case BreakDisposal.None:
                    break;
            }
        }

        private void DisableSelf() => gameObject.SetActive(false);

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (hiddenContent == null || !hiddenContent.scene.IsValid()) return;
            Gizmos.color = new Color(1f, 0.8f, 0f, 0.6f);
            Gizmos.DrawWireSphere(hiddenContent.transform.position, 0.3f);
            Gizmos.DrawLine(transform.position, hiddenContent.transform.position);
        }
#endif
    }

    /// <summary>What happens to the BreakableObject itself after breaking.</summary>
    public enum BreakDisposal
    {
        Destroy,
        Disable,
        None
    }
}
