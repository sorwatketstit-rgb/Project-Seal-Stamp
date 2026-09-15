using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace ProjectDawn.CozyBuilder
{
    /// <summary>
    /// Adds additional rows of quads at specific repeated heights. This detail was historically used to indicate flooring levels in walls.
    /// </summary>
    [AddComponentMenu("Cozy Builder/Modifier/Cozy String Courses")]
    public class CozyStringCourses : MonoBehaviour, ICozyGridModifier
    {
        [Tooltip("Defines the dimensions of each cell created by this modifier: width, height, and depth.")]
        [FormerlySerializedAs("MinPerimeter")]
        public float3 CellSize = new float3(0.25f, 0.05f, 0.3f);

        [Tooltip("The vertical spacing between rows of string courses, representing floors or structural divisions.")]
        public float Spacing = 1.5f;

        public void Modify(ref CozyBuilderContext context, NativeList<Cell> cells, CozyPlane plane)
        {
            float length = plane.SplineA.CalculateLength();

            int columnCount = (int)math.ceil(length / CellSize.x);
            columnCount = math.max(1, columnCount);

            float columnOffset = 0.5f / columnCount;

            for (int column = 0; column < columnCount; ++column)
            {
                float u = (float)(column + 0.5f) / (columnCount);

                float3 a = plane.SplineA.SampleSpline(u);
                float3 b = plane.SplineB.SampleSpline(u);
                float columnLength = math.distance(a, b);

                int rowCount = (int)math.floor(columnLength / Spacing);

                float rowOffset = CellSize.y / columnLength;

                for (int row = 0; row < rowCount; ++row)
                {
                    float v = (float)(row + 1) * Spacing / columnLength;
                    cells.Add(new Cell(new Rect(u - columnOffset, v - rowOffset, columnOffset * 2, rowOffset * 2), 0, CellSize.z, row, column));
                }
            }
        }

        public uint CalculateHash()
        {
            if (!isActiveAndEnabled)
                return 0;
            HashBuffer hash = new HashBuffer();
            hash.Write(CellSize);
            hash.Write(Spacing);
            return hash.GetHash();
        }
        void OnEnable() { }
    }
}