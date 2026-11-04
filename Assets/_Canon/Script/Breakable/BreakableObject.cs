using UnityEngine;
using UnityEngine.Events;

namespace SM64
{
    /// <summary>
    /// Attach this to any GameObject on the "Breakable" layer.
    /// When Break() is called (e.g. by BallBreakTrigger) it:
    ///   1. Plays an optional particle effect.
    ///   2. Reveals the hidden contents GameObject (if assigned).
    ///   3. Optionally destroys or disables itself.
    ///   4. Fires UnityEvents so designers can wire up extra reactions (sounds, score, etc).
    /// </summary>
    public class BreakableObject : MonoBehaviour
    {
        [Header("Hidden Contents")]
        [Tooltip("GameObject to reveal when this object is broken. Can be a child or a separate object in the scene.")]
        [SerializeField] private GameObject hiddenContent;

        [Tooltip("If true, the hidden content is activated in-place. If false, the content is simply set active.")]
        [SerializeField] private bool popOutContent = true;

        [Tooltip("Launch velocity applied to the hidden content's Rigidbody (if any) when revealed.")]
        [SerializeField] private Vector3 contentLaunchVelocity = new Vector3(0f, 5f, 0f);

        [Header("Break VFX")]
        [Tooltip("Particle effect prefab to spawn at this object's position on break.")]
        [SerializeField] private GameObject breakParticlePrefab;

        [Tooltip("How long (seconds) before the particle effect is auto-destroyed. 0 = never auto-destroy.")]
        [SerializeField] private float particleLifetime = 3f;

        [Header("Destroy / Disable")]
        [Tooltip("How to handle this object after breaking.")]
        [SerializeField] private BreakDisposal disposal = BreakDisposal.Destroy;

        [Tooltip("Delay before destroying / disabling this object (seconds). Lets particles finish first).")]
        [SerializeField] private float disposalDelay = 0.1f;

        [Header("State")]
        [SerializeField] private bool isBroken = false;

        [Header("Events")]
        public UnityEvent onBreak;

        /// <summary>True after Break() has been called once.</summary>
        public bool IsBroken => isBroken;

        // ─────────────────────── Public API ───────────────────────

        /// <summary>
        /// Called by BallBreakTrigger (or any other system) to break this object.
        /// Safe to call multiple times — only the first call does anything.
        /// </summary>
        public void Break()
        {
            if (isBroken) return;
            isBroken = true;

            SpawnBreakParticle();
            RevealContent();
            onBreak?.Invoke();
            DisposeThis();
        }

        // ─────────────────────── Internals ────────────────────────

        private void SpawnBreakParticle()
        {
            if (breakParticlePrefab == null) return;

            GameObject vfx = Instantiate(breakParticlePrefab, transform.position, transform.rotation);

            if (particleLifetime > 0f)
                Destroy(vfx, particleLifetime);
        }

        private void RevealContent()
        {
            if (hiddenContent == null) return;

            hiddenContent.SetActive(true);

            if (popOutContent && hiddenContent.TryGetComponent(out Rigidbody rb))
            {
                rb.linearVelocity = Vector3.zero;
                rb.AddForce(contentLaunchVelocity, ForceMode.VelocityChange);
            }
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
                    // keep the object alive — the designer handles it via onBreak events
                    break;
            }
        }

        private void DisableSelf() => gameObject.SetActive(false);

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (hiddenContent == null) return;
            Gizmos.color = new Color(1f, 0.8f, 0f, 0.6f);
            Gizmos.DrawWireSphere(hiddenContent.transform.position, 0.3f);
            Gizmos.DrawLine(transform.position, hiddenContent.transform.position);
        }
#endif
    }

    /// <summary>What happens to the BreakableObject itself after breaking.</summary>
    public enum BreakDisposal
    {
        /// <summary>Destroy the GameObject (default).</summary>
        Destroy,
        /// <summary>Only disable the GameObject — keeps it in the scene.</summary>
        Disable,
        /// <summary>Do nothing — handle disposal via onBreak events.</summary>
        None
    }
}

