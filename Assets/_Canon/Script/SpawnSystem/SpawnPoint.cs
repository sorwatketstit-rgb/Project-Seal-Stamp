using UnityEngine;

namespace SM64
{
    /// <summary>
    /// Represents the single spawn and respawn point in the game.
    /// Can be placed on any GameObject in the scene.
    /// Automatically registers itself as SpawnPoint.Instance.
    /// </summary>
    [DisallowMultipleComponent]
    public class SpawnPoint : MonoBehaviour
    {
        public static SpawnPoint Instance { get; private set; }

        [Header("Gizmo Settings")]
        [SerializeField] private Color gizmoColor = new Color(0.2f, 0.9f, 0.3f, 0.85f);
        [SerializeField] private float gizmoRadius = 0.5f;
        [SerializeField] private float arrowLength = 1.5f;

        public Vector3 Position => transform.position;
        public Quaternion Rotation => transform.rotation;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[SpawnPoint] Multiple SpawnPoints found in scene! Active spawn point is {Instance.gameObject.name}.", this);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        private void OnDisable()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Finds or discovers the active SpawnPoint in the scene.
        /// Searches SpawnPoint.Instance, then looks for a SpawnPoint component,
        /// then searches for a GameObject named "SpawnPoint", "Spawn Point", or tagged "Respawn".
        /// </summary>
        public static SpawnPoint GetSpawnPoint()
        {
            if (Instance != null) return Instance;

            Instance = FindFirstObjectByType<SpawnPoint>();
            if (Instance != null) return Instance;

            GameObject go = GameObject.Find("SpawnPoint") ?? GameObject.Find("Spawn Point") ?? GameObject.FindWithTag("Respawn");
            if (go != null)
            {
                Instance = go.GetComponent<SpawnPoint>() ?? go.AddComponent<SpawnPoint>();
                return Instance;
            }

            return null;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, gizmoRadius);
            Gizmos.DrawRay(transform.position, transform.forward * arrowLength);

            // Draw a direction pointer arrow head
            Vector3 tip = transform.position + transform.forward * arrowLength;
            Vector3 right = Quaternion.Euler(0, 150, 0) * transform.forward;
            Vector3 left = Quaternion.Euler(0, -150, 0) * transform.forward;
            Gizmos.DrawRay(tip, right * 0.4f);
            Gizmos.DrawRay(tip, left * 0.4f);
        }
    }
}

