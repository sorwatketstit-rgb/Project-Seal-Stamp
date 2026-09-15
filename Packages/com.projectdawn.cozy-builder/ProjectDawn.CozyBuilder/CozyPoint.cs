using Unity.Mathematics;
using UnityEngine;

namespace ProjectDawn.CozyBuilder
{
    [AddComponentMenu("Cozy Builder/Geometry/Cozy Point")]
    public class CozyPoint : MonoBehaviour, ICozyHashable
    {
        public float3 Position { get => transform.position; set => transform.position = value; }

#if UNITY_EDITOR
        public void DrawPoint(Color color, float size)
        {
            Gizmos.color = color;
            CozyGizmos.DrawPoint(Position, size);
        }
#endif

        public uint CalculateHash()
        {
            unsafe
            {
                float3 position = transform.position;
                return math.hash(&position, sizeof(float3));
            }
        }
    }
}