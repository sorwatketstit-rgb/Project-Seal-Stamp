using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace ProjectDawn.CozyBuilder
{
    public interface ICozyGridModifier : ICozyHashable
    {
        void Modify(ref CozyBuilderContext context, NativeList<Cell> cells, CozyPlane plane);
    }

    [AddComponentMenu("Cozy Builder/Builder/Cozy Grid")]
    public class CozyGrid : MonoBehaviour, ICozyBuilder
    {
        [Tooltip("Size of the grid cells.")]
        [FormerlySerializedAs("MinPerimeter")]
        public float3 CellSize = 0.2f;

        [Tooltip("Controls the vertical centering of the grid.")]
        [Range(0.0f, 1.0f)]
        public float CenterV = 0.5f;

        CozyPlane Plane => GetComponent<CozyPlane>() ?? transform.parent?.GetComponent<CozyPlane>();

        public uint CalculateHash()
        {
            if (!isActiveAndEnabled)
                return 0;
            if (Plane == null)
                return 0;
            HashBuffer hash = new HashBuffer();
            using (var modifiers = this.GetComponentsScoped<ICozyGridModifier>(true))
                foreach (var modifier in modifiers.List)
                    if (modifier is MonoBehaviour component && component.isActiveAndEnabled)
                        hash.Write(modifier.CalculateHash());
            hash.Write(Plane.CalculateHash());
            hash.Write(CellSize);
            hash.Write(CenterV);
            return hash.GetHash();
        }

        void ICozyBuilder.Build(ref CozyBuilderContext context)
        {
            float rowLength = math.min(Plane.SplineA.CalculateLength(), Plane.SplineB.CalculateLength());

            int rowCount = (int)math.ceil(rowLength / CellSize.x);
            rowCount = math.max(1, rowCount);

            float rowOffset = 0.5f / rowCount;

            float centerOffset = math.lerp(-rowOffset, rowOffset, CenterV);

            using NativeList<Cell> cells = new NativeList<Cell>(Allocator.Temp);

            for (int column = 0; column < rowCount; ++column)
            {
                float u =  (float)(column + 0.5f) / (rowCount);

                float3 a = Plane.SplineA.SampleSpline(u + centerOffset);
                float3 b = Plane.SplineB.SampleSpline(u + centerOffset);
                float columnLength = math.distance(a, b);

                int columnCount = (int)math.ceil(columnLength / CellSize.y);
                columnCount = math.max(1, columnCount);

                float columnOffset = 0.5f / columnCount;

                for (int row = 0; row < columnCount; ++row)
                {
                    float v = (float)(row + 0.5f) / (columnCount);
                    cells.Add(new Cell(new Rect(u - rowOffset, v - columnOffset, rowOffset * 2, columnOffset * 2), 0, CellSize.z, row, column));
                }
            }

            if (cells.Length == 0)
                return;

            using (var modifiers = this.GetComponentsScoped<ICozyGridModifier>(true))
                foreach (var modifier in modifiers.List)
                    if (modifier is MonoBehaviour component && component.isActiveAndEnabled && modifier.CalculateHash() != 0)
                        modifier.Modify(ref context, cells, Plane);
        }
    }
}