using UnityEngine;

namespace SM64
{
    /// <summary>
    /// Attach this to a mail letter GameObject in the Collectables layer with the tag "Letter".
    /// - Floats up and down (Mario-coin style) and slowly rotates.
    /// - Uses an isTrigger Collider3D - when the Player enters, PlayerInventory handles pickup.
    /// - Call Collect() externally (from PlayerInventory.OnTriggerEnter) to trigger the effect
    ///   and destroy this object.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class MailLetterCollectable : MonoBehaviour
    {
        [Header("Float Animation")]
        [Tooltip("Amplitude of the up-down float in world units.")]
        public float floatAmplitude = 0.35f;

        [Tooltip("Speed of the up-down float cycle (oscillations per second).")]
        public float floatFrequency = 1.2f;

        [Tooltip("Degrees per second the letter spins on its Y axis.")]
        public float rotationSpeed = 90f;

        [Header("Collection")]
        [Tooltip("Optional particle effect spawned at collection point.")]
        public GameObject collectEffectPrefab;

        [Tooltip("Optional audio clip played on collection.")]
        public AudioClip collectSound;

        // Internal
        private Vector3 _originPosition;
        private bool _collected = false;

        /// <summary>False once Collect() has been called; prevents double-pickup.</summary>
        public bool CanBeCollected => !_collected;

        // -----------------------------------------------------------------------
        //  Unity lifecycle
        // -----------------------------------------------------------------------

        private void Awake()
        {
            // Make sure the collider is a trigger
            Collider col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void Start()
        {
            _originPosition = transform.position;

            // Stagger float phase so multiple letters don't all bob in sync
            // by offsetting the time based on world position hash
            float phaseOffset = (transform.position.x + transform.position.z) % (2f * Mathf.PI);
            _originPosition = transform.position;
            // Store a randomised phase-offset into the sin wave via position shift trick
            _originPosition.y -= Mathf.Sin(phaseOffset) * floatAmplitude;
        }

        private void Update()
        {
            if (_collected) return;

            // Floating bob
            float newY = _originPosition.y + Mathf.Sin(Time.time * floatFrequency * 2f * Mathf.PI) * floatAmplitude;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);

            // Slow spin
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }

        // -----------------------------------------------------------------------
        //  Collection
        // -----------------------------------------------------------------------

        /// <summary>
        /// Called by PlayerInventory when the player overlaps this trigger.
        /// Plays effects and destroys the GameObject.
        /// </summary>
        public void Collect()
        {
            if (_collected) return;
            _collected = true;

            // Spawn visual effect
            if (collectEffectPrefab != null)
            {
                Instantiate(collectEffectPrefab, transform.position, Quaternion.identity);
            }

            // Play sound via a temporary AudioSource at the collect position
            if (collectSound != null)
            {
                AudioSource.PlayClipAtPoint(collectSound, transform.position);
            }

            Destroy(gameObject);
        }

        // -----------------------------------------------------------------------
        //  Editor gizmo so you can see the float range in Scene view
        // -----------------------------------------------------------------------
        private void OnDrawGizmosSelected()
        {
            Vector3 center = Application.isPlaying ? _originPosition : transform.position;
            Gizmos.color = new Color(1f, 0.8f, 0f, 0.4f);
            Gizmos.DrawLine(center + Vector3.down * floatAmplitude,
                            center + Vector3.up * floatAmplitude);
        }
    }
}
