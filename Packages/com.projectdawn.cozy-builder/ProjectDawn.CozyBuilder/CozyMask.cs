using Unity.Mathematics;
using UnityEngine;

namespace ProjectDawn.CozyBuilder
{
    [AddComponentMenu("Cozy Builder/Cozy Mask")]
    public class CozyMask : MonoBehaviour, ICozyHashable
    {
        [Tooltip("Offset for the mask's position.")]
        public float3 Offset = 0;

        [Tooltip("Size of the mask.")]
        public float2 Size = 1.0f;

        public uint CalculateHash()
        {
            if (!isActiveAndEnabled)
                return 0;
            HashBuffer hash = new HashBuffer();
            hash.Write(transform.position);
            hash.Write(Offset);
            hash.Write(Size);
            return hash.GetHash();
        }
    }
}