using Unity.Mathematics;
using UnityEngine;

namespace ProjectDawn.CozyBuilder
{
    [AddComponentMenu("Cozy Builder/Builder/Cozy Attachment")]
    public class CozyAttachment : MonoBehaviour, ICozyBuilder
    {
        [Tooltip("The prefab to be instantiated.")]
        public GameObject Prefab;

        [Tooltip("Offset from the calculated attachment position.")]
        public float3 Offset = 0.0f;

        [Tooltip("Size of the attachment area on the plane.")]
        public float2 Size = 1.0f;

        CozyPlane Plane => transform.parent?.GetComponent<CozyPlane>() ?? GetComponent<CozyPlane>();

        public void Build(ref CozyBuilderContext context)
        {
            Rect rect = Plane.GetRect(transform.position, Size);

            float3 p0 = Plane.SamplePlane(new float2(rect.x + rect.width, rect.y + rect.height * 0.5f));
            float3 p1 = Plane.SamplePlane(new float2(rect.x, rect.y + rect.height * 0.5f));
            float3 p2 = Plane.SamplePlane(new float2(rect.x + rect.width * 0.5f, rect.y + rect.height));
            float3 p3 = Plane.SamplePlane(new float2(rect.x + rect.width * 0.5f, rect.y));

            float3 center = (p0 + p1 + p2 + p3) * 0.25f;

            float3 right = -math.normalizesafe(p0 - p1);
            float3 up = math.normalizesafe(p2 - p3);
            float3 forward = math.cross(right, up);
            quaternion rotation = quaternion.LookRotation(forward, up);

            center += math.rotate(rotation, Offset);

            GameObject prefab = Prefab;
            context.Instantiate(prefab, center, rotation, 1);
        }

        public uint CalculateHash()
        {
            if (!isActiveAndEnabled)
                return 0;
            if (!Plane)
                return 0;
            HashBuffer hash = new HashBuffer();
            hash.Write(Plane.CalculateHash());
            hash.Write(transform.position);
            hash.Write(Size);
            hash.Write(Offset);
            return hash.GetHash();
        }
    }
}