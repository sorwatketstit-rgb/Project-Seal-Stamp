using UnityEngine;

namespace SM64
{
    /// <summary>
    /// Renders a 3D Quad crosshair in world space at the camera's aim point.
    /// Does NOT use a Canvas — it is a true 3D Quad mesh created at runtime.
    /// Assign any Material in the Inspector to customise its look.
    /// Dynamically scales so it stays a consistent size on screen at any distance.
    /// </summary>
    public class CrosshairPlane : MonoBehaviour
    {
        [Header("Material")]
        [Tooltip("Material to apply to the auto-created crosshair quad. Leave empty to use a built-in procedural reticle.")]
        [SerializeField] private Material crosshairMaterial;

        [Header("Appearance & Sizing")]
        [Tooltip("Color tint applied to the crosshair. Only used for the procedural reticle (when no Material is assigned).")]
        [SerializeField] private Color crosshairColor = new Color(1f, 1f, 1f, 0.95f);

        [Tooltip("Base size of the crosshair plane in meters at 10m distance.")]
        [SerializeField] private float baseScale = 0.35f;

        [Header("Compensation & Fine-Tuning")]
        [Tooltip("Fine-tune screen offset for the crosshair in camera space (X = right/left, Y = up/down).")]
        [SerializeField] private Vector2 screenCompensationOffset = Vector2.zero;

        [Header("Targeting")]
        [Tooltip("Layers that the crosshair raycast will stick to.")]
        [SerializeField] private LayerMask hitLayers = ~0;

        [Tooltip("Max raycast distance for placing the crosshair on surfaces.")]
        [SerializeField] private float maxDistance = 60f;

        [Tooltip("Distance in front of the camera when aiming into the sky or nothing is hit.")]
        [SerializeField] private float defaultDistance = 15f;

        private GameObject _crosshairObject;
        private Material _proceduralMaterial;
        private Texture2D _proceduralTexture;
        private Camera _mainCamera;

        public Vector3 CurrentAimPoint { get; private set; }

        /// <summary>
        /// Swap the material at runtime (or from the Inspector via context menu).
        /// </summary>
        public void SetMaterial(Material mat)
        {
            crosshairMaterial = mat;
            if (_crosshairObject != null)
            {
                MeshRenderer mr = _crosshairObject.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = ResolvedMaterial();
            }
        }

        private void Awake()
        {
            EnsureCrosshairObject();
        }

        private void Start()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            Hide();
        }

        public void Show()
        {
            if (_crosshairObject == null) EnsureCrosshairObject();
            if (_crosshairObject != null) _crosshairObject.SetActive(true);
        }

        public void Hide()
        {
            if (_crosshairObject != null) _crosshairObject.SetActive(false);
        }

        /// <summary>
        /// Updates the 3D position, rotation, and dynamic scale of the crosshair plane.
        /// </summary>
        public void UpdateAim(Camera cam = null)
        {
            if (cam == null) cam = _mainCamera != null ? _mainCamera : Camera.main;
            if (cam == null) return;

            Show();

            Vector3 rayOrigin = cam.transform.position;
            Vector3 rayDir = cam.transform.forward;

            if (screenCompensationOffset.sqrMagnitude > 0.0001f)
            {
                Vector3 offsetVector = cam.transform.right * screenCompensationOffset.x + cam.transform.up * screenCompensationOffset.y;
                rayDir = (cam.transform.forward + offsetVector * 0.05f).normalized;
            }

            Ray ray = new Ray(rayOrigin, rayDir);
            Vector3 aimPos;

            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, hitLayers, QueryTriggerInteraction.Ignore))
            {
                aimPos = hit.point - ray.direction * 0.05f;
            }
            else
            {
                aimPos = ray.origin + ray.direction * defaultDistance;
            }

            CurrentAimPoint = aimPos;

            if (_crosshairObject != null)
            {
                _crosshairObject.transform.position = aimPos;
                // Face the camera directly
                _crosshairObject.transform.rotation = Quaternion.LookRotation(cam.transform.forward);

                // Constant perceived size on screen
                float dist = Vector3.Distance(cam.transform.position, aimPos);
                float scale = baseScale * Mathf.Max(dist / 10f, 0.1f);
                _crosshairObject.transform.localScale = Vector3.one * scale;
            }
        }

        private void EnsureCrosshairObject()
        {
            if (_crosshairObject != null) return;

            // Create a Quad primitive
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Crosshair_Plane";

            // Remove collider so it never interacts with physics or raycasts
            Collider col = quad.GetComponent<Collider>();
            if (col != null) Destroy(col);

            MeshRenderer meshRenderer = quad.GetComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.sharedMaterial = ResolvedMaterial();

            _crosshairObject = quad;
        }

        /// <summary>
        /// Returns the material to use: assigned material, or a generated procedural reticle.
        /// </summary>
        private Material ResolvedMaterial()
        {
            if (crosshairMaterial != null) return crosshairMaterial;

            // Build a one-time procedural reticle material
            if (_proceduralMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                             ?? Shader.Find("Sprites/Default")
                             ?? Shader.Find("Unlit/Transparent")
                             ?? Shader.Find("Mobile/Particles/Alpha Blended")
                             ?? Shader.Find("Standard");

                _proceduralMaterial = new Material(shader);

                if (_proceduralTexture == null)
                    _proceduralTexture = GenerateReticleTexture();

                _proceduralMaterial.mainTexture = _proceduralTexture;

                if (_proceduralMaterial.HasProperty("_Color"))    _proceduralMaterial.SetColor("_Color",     crosshairColor);
                if (_proceduralMaterial.HasProperty("_BaseColor")) _proceduralMaterial.SetColor("_BaseColor", crosshairColor);
            }

            return _proceduralMaterial;
        }

        private Texture2D GenerateReticleTexture()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color solid = Color.white;
            float center = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center));
                    float alpha = 0f;

                    // Center dot (radius 2.5)
                    if (dist <= 3.2f)
                    {
                        alpha = Mathf.Clamp01(3.2f - dist);
                    }

                    // Outer ring (radius ~32 to 36)
                    float ringDist = Mathf.Abs(dist - 34f);
                    if (ringDist <= 2.2f)
                    {
                        alpha = Mathf.Max(alpha, Mathf.Clamp01(2.2f - ringDist));
                    }

                    // 4 notch lines (crosshair ticks: left, right, top, bottom)
                    float dx = Mathf.Abs(x + 0.5f - center);
                    float dy = Mathf.Abs(y + 0.5f - center);

                    // Horizontal ticks
                    if (dy <= 1.5f && dist >= 38f && dist <= 48f)
                    {
                        alpha = Mathf.Max(alpha, Mathf.Clamp01(1.5f - dy));
                    }
                    // Vertical ticks
                    if (dx <= 1.5f && dist >= 38f && dist <= 48f)
                    {
                        alpha = Mathf.Max(alpha, Mathf.Clamp01(1.5f - dx));
                    }

                    tex.SetPixel(x, y, new Color(solid.r, solid.g, solid.b, alpha));
                }
            }

            tex.Apply();
            return tex;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_crosshairObject != null)
            {
                MeshRenderer mr = _crosshairObject.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = ResolvedMaterial();
            }
        }
#endif

        private void OnDestroy()
        {
            if (_crosshairObject != null)
            {
                Destroy(_crosshairObject);
            }
            if (_proceduralMaterial != null)
            {
                Destroy(_proceduralMaterial);
            }
            if (_proceduralTexture != null)
            {
                Destroy(_proceduralTexture);
            }
        }
    }
}

