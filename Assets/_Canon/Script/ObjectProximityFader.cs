using UnityEngine;

namespace SM64
{
    /// <summary>
    /// Component placed on GameObjects (such as the player character or obstacles) to manage
    /// proximity fade without mutating shared materials.
    /// Uses MaterialPropertyBlock to avoid runtime material leaks or asset modification.
    /// Works seamlessly with the CameraProximityDitherLit shader.
    /// </summary>
    public class ObjectProximityFader : MonoBehaviour
    {
        [Header("Renderers to Fade")]
        [Tooltip("Child renderers to apply proximity fading to. If empty, automatically gathers all child renderers in Awake.")]
        [SerializeField] private Renderer[] targetRenderers;

        [Header("Distance Thresholds (0 = Use Camera Global)")]
        [Tooltip("Distance from camera at which this object is completely invisible/dithered away.")]
        [SerializeField] private float fadeNearDistance = 0f;

        [Tooltip("Distance from camera at which this object begins fading out.")]
        [SerializeField] private float fadeFarDistance = 0f;

        [Tooltip("Manual alpha/opacity multiplier (0 = invisible, 1 = fully visible).")]
        [Range(0f, 1f)]
        [SerializeField] private float alphaMultiplier = 1.0f;

        private MaterialPropertyBlock _propBlock;
        private static readonly int FadeNearId = Shader.PropertyToID("_FadeNearDistance");
        private static readonly int FadeFarId = Shader.PropertyToID("_FadeFarDistance");
        private static readonly int AlphaMultiplierId = Shader.PropertyToID("_AlphaMultiplier");

        private void Awake()
        {
            if (targetRenderers == null || targetRenderers.Length == 0)
            {
                targetRenderers = GetComponentsInChildren<Renderer>(true);
            }

            _propBlock = new MaterialPropertyBlock();
            ApplyProperties();
        }

        private void OnValidate()
        {
            if (_propBlock == null)
            {
                _propBlock = new MaterialPropertyBlock();
            }
            ApplyProperties();
        }

        /// <summary>
        /// Updates the MaterialPropertyBlock on all registered renderers.
        /// </summary>
        public void ApplyProperties()
        {
            if (targetRenderers == null) return;

            for (int i = 0; i < targetRenderers.Length; i++)
            {
                Renderer r = targetRenderers[i];
                if (r == null) continue;

                r.GetPropertyBlock(_propBlock);
                _propBlock.SetFloat(FadeNearId, fadeNearDistance);
                _propBlock.SetFloat(FadeFarId, fadeFarDistance);
                _propBlock.SetFloat(AlphaMultiplierId, alphaMultiplier);
                r.SetPropertyBlock(_propBlock);
            }
        }

        /// <summary>
        /// Dynamically alters the alpha multiplier for this object.
        /// </summary>
        public void SetAlpha(float alpha)
        {
            alphaMultiplier = Mathf.Clamp01(alpha);
            ApplyProperties();
        }

        /// <summary>
        /// Sets custom near and far fade distances for this specific object.
        /// </summary>
        public void SetFadeDistances(float near, float far)
        {
            fadeNearDistance = near;
            fadeFarDistance = far;
            ApplyProperties();
        }
    }
}
