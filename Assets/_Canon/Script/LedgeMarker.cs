using UnityEngine;

namespace SM64
{
    /// <summary>
    /// Optional marker placed on a ledge plane or object to define explicit
    /// hang and climb-up destinations for the player.
    /// </summary>
    public class LedgeMarker : MonoBehaviour
    {
        [Header("Marker Transforms (Optional)")]
        [Tooltip("Exact transform where the player character hangs. If empty, calculated relative to marker position.")]
        public Transform hangTransform;

        [Tooltip("Exact transform where the player character lands after climbing up. If empty, placed on top of the marker.")]
        public Transform climbTransform;

        [Header("Manual Offsets (Used if transforms are not assigned)")]
        public Vector3 hangOffset = new Vector3(0f, -1.0f, -0.4f);
        public Vector3 climbOffset = new Vector3(0f, 0.1f, 0.4f);

        public Vector3 GetHangPosition()
        {
            if (hangTransform != null)
                return hangTransform.position;

            return transform.position + transform.TransformDirection(hangOffset);
        }

        public Vector3 GetClimbPosition()
        {
            if (climbTransform != null)
                return climbTransform.position;

            return transform.position + transform.TransformDirection(climbOffset);
        }

        public Vector3 GetWallNormal()
        {
            // By default, facing outward from the marker's forward direction or negative forward
            return -transform.forward;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, new Vector3(0.6f, 0.05f, 0.6f));

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(GetHangPosition(), 0.12f);

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(GetClimbPosition(), 0.12f);

            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(GetHangPosition(), GetClimbPosition() - GetHangPosition());
        }
    }
}
