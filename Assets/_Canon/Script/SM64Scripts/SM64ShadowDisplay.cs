using UnityEngine;
#if ENABLE_URP
using UnityEngine.Rendering.Universal;
#endif

namespace SM64
{
    /// <summary>
    /// Projects a circular blob shadow beneath the character in the style of Super Mario 64.
    /// Casts a raycast straight down to locate solid ground when grounded or airborne,
    /// positioning and orienting the shadow decal along the surface normal.
    /// Dynamically scales and fades the shadow as the player ascends into the air.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)] // Runs after player controller movement in LateUpdate
    public class SM64ShadowDisplay : MonoBehaviour
    {
        [Header("Target & Ground Raycast")]
        [Tooltip("Transform to cast the shadow from. Defaults to this GameObject.")]
        [SerializeField] private Transform targetTransform;

        [Tooltip("Height above targetTransform position from which the downward raycast begins.")]
        [SerializeField] private float raycastOriginHeight = 0.5f;

        [Tooltip("Maximum vertical distance to search for solid ground beneath the player.")]
        [SerializeField] private float maxShadowDistance = 30f;

        [Tooltip("Layers considered solid ground for the shadow decal.")]
        [SerializeField] private LayerMask groundLayerMask = ~0;

        [Tooltip("Small normal offset above the ground surface to prevent Z-fighting with floor polygons.")]
        [SerializeField] private float surfaceOffset = 0.02f;

        [Header("Sizing & Scaling")]
        [Tooltip("Base radius/scale of the shadow when the player is on the ground.")]
        [SerializeField] private float baseScale = 1.3f;

        [Tooltip("Minimum scale of the shadow when the player reaches maximum airborne height.")]
        [SerializeField] private float minScaleAtMaxHeight = 0.6f;

        [Header("Opacity & Color")]
        [Tooltip("Shadow tint color.")]
        [SerializeField] private Color shadowColor = new Color(0.04f, 0.04f, 0.08f, 1f);

        [Tooltip("Base opacity when grounded (0 = transparent, 1 = solid).")]
        [Range(0f, 1f)]
        [SerializeField] private float baseOpacity = 0.65f;

        [Tooltip("Minimum opacity when the player reaches maximum airborne height.")]
        [Range(0f, 1f)]
        [SerializeField] private float minOpacityAtMaxHeight = 0.15f;

        [Header("Orientation & Smoothing")]
        [Tooltip("Whether the shadow rotates to match the normal/slope of the surface it hits.")]
        [SerializeField] private bool alignWithGroundNormal = true;

        [Tooltip("Smoothing speed for shadow movement (0 = instant snap).")]
        [SerializeField] private float followSmoothing = 0f;

        [Header("Visual Object (Optional)")]
        [Tooltip("Custom shadow GameObject. If unassigned, an optimal circular shadow quad is generated automatically at runtime.")]
        [SerializeField] private GameObject shadowObject;

        [Tooltip("Custom material for the shadow quad. If unassigned, uses the SM64 blob shadow material.")]
        [SerializeField] private Material shadowMaterial;

        // Cached runtime references
        private SM64PlayerController _playerController;
        private Transform _shadowTransform;
        private MeshRenderer _shadowMeshRenderer;
        private MaterialPropertyBlock _propBlock;
        private bool _isAutoCreated;
        private bool _hasHitGround;
        private float _currentGroundDistance;
        private RaycastHit _lastHit;

        // Non-alloc raycast buffer
        private readonly RaycastHit[] _raycastHits = new RaycastHit[12];
        private static readonly int ColorPropId = Shader.PropertyToID("_Color");

        // Public Accessors
        public bool HasHitGround => _hasHitGround;
        public float CurrentGroundDistance => _currentGroundDistance;
        public RaycastHit LastHit => _lastHit;
        public GameObject ShadowObject => shadowObject;

        private void Awake()
        {
            if (targetTransform == null)
            {
                targetTransform = transform;
            }

            _playerController = GetComponentInParent<SM64PlayerController>() ?? GetComponent<SM64PlayerController>();
            if (_playerController != null && groundLayerMask.value == ~0 && _playerController.groundMask.value != 0)
            {
                // Inherit ground mask from player controller if left at default everything
                groundLayerMask = _playerController.groundMask;
            }

            _propBlock = new MaterialPropertyBlock();
            InitializeShadowObject();
        }

        private void InitializeShadowObject()
        {
            if (shadowObject == null)
            {
                // Create an auto-generated circular quad GameObject detached from parent rotation
                shadowObject = new GameObject($"{name}_SM64BlobShadow");
                _isAutoCreated = true;

                MeshFilter mf = shadowObject.AddComponent<MeshFilter>();
                mf.sharedMesh = CreateFlatQuadMesh();

                _shadowMeshRenderer = shadowObject.AddComponent<MeshRenderer>();
                _shadowMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _shadowMeshRenderer.receiveShadows = false;
                _shadowMeshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                _shadowMeshRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

                // Assign material
                if (shadowMaterial == null)
                {
                    shadowMaterial = Resources.Load<Material>("SM64_BlobShadow");
                    if (shadowMaterial == null)
                    {
                        Shader shader = Shader.Find("SM64/BlobShadow") ?? Shader.Find("Universal Render Pipeline/Unlit");
                        shadowMaterial = new Material(shader) { name = "Runtime_SM64BlobShadow" };
                    }
                }
                _shadowMeshRenderer.sharedMaterial = shadowMaterial;
            }
            else
            {
                _shadowMeshRenderer = shadowObject.GetComponentInChildren<MeshRenderer>();
            }

            _shadowTransform = shadowObject.transform;
            shadowObject.SetActive(false);
        }

        private void LateUpdate()
        {
            UpdateShadowPosition();
        }

        private void UpdateShadowPosition()
        {
            if (targetTransform == null || _shadowTransform == null) return;

            Vector3 origin = targetTransform.position + Vector3.up * raycastOriginHeight;
            Ray ray = new Ray(origin, Vector3.down);

            int hitCount = Physics.RaycastNonAlloc(
                ray,
                _raycastHits,
                maxShadowDistance + raycastOriginHeight,
                groundLayerMask,
                QueryTriggerInteraction.Ignore
            );

            float closestDistance = float.MaxValue;
            RaycastHit closestHit = default;
            bool hitFound = false;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = _raycastHits[i];
                if (hit.collider == null) continue;

                // Ignore player's own colliders
                if (hit.collider.transform == targetTransform || hit.collider.transform.IsChildOf(targetTransform))
                    continue;

                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    closestHit = hit;
                    hitFound = true;
                }
            }

            _hasHitGround = hitFound;

            if (hitFound)
            {
                _lastHit = closestHit;
                float verticalDistance = Mathf.Max(0f, closestDistance - raycastOriginHeight);
                _currentGroundDistance = verticalDistance;

                float heightFactor = Mathf.Clamp01(verticalDistance / maxShadowDistance);

                // Calculate dynamic size and opacity based on height above ground
                float currentScale = Mathf.Lerp(baseScale, minScaleAtMaxHeight, heightFactor);
                float currentOpacity = Mathf.Lerp(baseOpacity, minOpacityAtMaxHeight, heightFactor);

                // Position on ground surface with normal offset
                Vector3 targetPos = closestHit.point + closestHit.normal * surfaceOffset;

                // Orientation aligning with ground normal
                Quaternion targetRot = alignWithGroundNormal
                    ? Quaternion.FromToRotation(Vector3.up, closestHit.normal)
                    : Quaternion.identity;

                Vector3 targetScaleVec = new Vector3(currentScale, 1f, currentScale);

                if (followSmoothing > 0f && Application.isPlaying)
                {
                    float delta = Time.deltaTime * followSmoothing;
                    _shadowTransform.position = Vector3.Lerp(_shadowTransform.position, targetPos, delta);
                    _shadowTransform.rotation = Quaternion.Slerp(_shadowTransform.rotation, targetRot, delta);
                    _shadowTransform.localScale = Vector3.Lerp(_shadowTransform.localScale, targetScaleVec, delta);
                }
                else
                {
                    _shadowTransform.position = targetPos;
                    _shadowTransform.rotation = targetRot;
                    _shadowTransform.localScale = targetScaleVec;
                }

                // Update opacity via MaterialPropertyBlock (no GC / material cloning)
                if (_shadowMeshRenderer != null)
                {
                    _shadowMeshRenderer.GetPropertyBlock(_propBlock);
                    Color finalColor = new Color(shadowColor.r, shadowColor.g, shadowColor.b, shadowColor.a * currentOpacity);
                    _propBlock.SetColor(ColorPropId, finalColor);
                    _shadowMeshRenderer.SetPropertyBlock(_propBlock);
                }

                if (!shadowObject.activeSelf)
                {
                    shadowObject.SetActive(true);
                }
            }
            else
            {
                _currentGroundDistance = float.PositiveInfinity;
                if (shadowObject.activeSelf)
                {
                    shadowObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Creates a unit quad mesh lying flat on the XZ plane with upward-facing normal.
        /// </summary>
        private static Mesh CreateFlatQuadMesh()
        {
            Mesh mesh = new Mesh { name = "SM64_ShadowQuadMesh" };

            Vector3[] vertices = new Vector3[]
            {
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3( 0.5f, 0f, -0.5f),
                new Vector3(-0.5f, 0f,  0.5f),
                new Vector3( 0.5f, 0f,  0.5f)
            };

            Vector2[] uvs = new Vector2[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            };

            Vector3[] normals = new Vector3[]
            {
                Vector3.up,
                Vector3.up,
                Vector3.up,
                Vector3.up
            };

            int[] triangles = new int[]
            {
                0, 2, 1,
                2, 3, 1
            };

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.normals = normals;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();

            return mesh;
        }

        private void OnEnable()
        {
            if (shadowObject != null)
            {
                shadowObject.SetActive(_hasHitGround);
            }
        }

        private void OnDisable()
        {
            if (shadowObject != null)
            {
                shadowObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (_isAutoCreated && shadowObject != null)
            {
                Destroy(shadowObject);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = (targetTransform != null ? targetTransform.position : transform.position) + Vector3.up * raycastOriginHeight;
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(origin, Vector3.down * maxShadowDistance);

            if (_hasHitGround)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(_lastHit.point, 0.2f);
            }
        }
    }
}

