using Unity.Collections;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace ProjectDawn.CozyBuilder
{
    /// <summary>
    /// Rearranges cell widths to create a brick-like pattern and occasionally splits bricks.
    /// </summary>
    [AddComponentMenu("Cozy Builder/Modifier/Cozy Double Lap Tilling")]
    public class CozyDoubleLapTilling : MonoBehaviour, ICozyGridModifier
    {
        [Tooltip("The seed value for randomizing the scale of cells.")]
        public uint Seed = 1;

        [Tooltip("The range for scaling the width of cells.")]
        [MinMaxRange(0, 2)]
        public Range WidthScale = new Range(0.8f, 1.2f);

        [Tooltip("The range for scaling the height of cells.")]
        [MinMaxRange(0, 2)]
        public Range HeightScale = new Range(1.0f, 1.0f);

        [Tooltip("The range for scaling the depth of cells.")]
        [MinMaxRange(0, 2)]
        public Range DepthScale = new Range(0.8f, 1.2f);

        public void Modify(ref CozyBuilderContext context, NativeList<Cell> cells, CozyPlane plane)
        {
            Random random = new Random(Seed);
            for (int i = 0; i < cells.Length; i++)
            {
                ref Cell cell = ref cells.ElementAt(i);
                cell.Rect.width *= random.NextFloat(WidthScale.Start, WidthScale.End);
                cell.Rect.height *= random.NextFloat(HeightScale.Start, HeightScale.End);
                cell.Depth *= random.NextFloat(DepthScale.Start, DepthScale.End);
            }
        }

        public uint CalculateHash()
        {
            HashBuffer hash = new HashBuffer();
            hash.Write(Seed);
            hash.Write(WidthScale);
            hash.Write(HeightScale);
            hash.Write(DepthScale);
            return hash.GetHash();
        }

        void OnEnable() { }
    }
}