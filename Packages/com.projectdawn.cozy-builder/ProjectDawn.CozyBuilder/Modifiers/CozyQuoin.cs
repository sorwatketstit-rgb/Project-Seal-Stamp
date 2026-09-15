using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace ProjectDawn.CozyBuilder
{
    /// <summary>
    /// Quoin adds bricks at the corners of the plane.
    /// </summary>
    [AddComponentMenu("Cozy Builder/Modifier/Cozy Quoin")]
    public class CozyQuoin : MonoBehaviour, ICozyGridModifier
    {
        [Tooltip("The range of angles (in degrees) at the corners where bricks will be added.")]
        [MinMaxRange(0, 180)]
        public Range CornerAngle = new Range(80, 100);

        float Z = 0;

        [Tooltip("The width of the quoin brick section as a proportion of the total row length.")]
        public float Width = 0.6f;

        [Tooltip("The height of each quoin brick section.")]
        public float Height = 0.3f;

        [Tooltip("The depth of the quoin brick section.")]
        public float Depth = 0.16f;

        [Tooltip("If enabled, mirrors the quoin pattern on the opposite side of the plane.")]
        public bool Mirror = true;

        public void Modify(ref CozyBuilderContext context, NativeList<Cell> cells, CozyPlane plane)
        {
            using NativeList<Cell> newQuads = new NativeList<Cell>(Allocator.Temp);

            float rowLength = plane.CalculateRowLength();

            int rowCount = plane.SplineA.Points.Count;
            rowCount = math.max(1, rowCount) + 1;

            int index = 0;

            for (int column = 0; column < rowCount - 1; column++)
            {
                float u = plane.SplineA.GetPointTAtIndex(column);

                float columnLength = plane.CalculateColumnLength(u);

                Rect quoinRect = new Rect(u - Width / rowLength * 0.5f, 0, Width / rowLength, 1);

                float3 p0 = plane.SamplePlane(new float2(quoinRect.x + quoinRect.width, quoinRect.y + quoinRect.height * 0.5f));
                float3 p1 = plane.SamplePlane(new float2(quoinRect.x, quoinRect.y + quoinRect.height * 0.5f));
                float3 p2 = plane.SamplePlane(new float2(quoinRect.x + quoinRect.width * 0.5f, quoinRect.y + quoinRect.height));
                float3 p3 = plane.SamplePlane(new float2(quoinRect.x + quoinRect.width * 0.5f, quoinRect.y));

                float3 center = plane.SamplePlane(quoinRect.center);

                float cosAngle = math.dot(math.normalizesafe(p1 - center), math.normalizesafe(p0 - center));
                float angle = math.degrees(math.acos(cosAngle));
                if (CornerAngle.Start > angle || angle > CornerAngle.End)
                    continue;

                int columnSegments = (int)(columnLength / Height);

                float uOffsetByDepth = Depth / rowLength * 0.5f;
                //uOffsetByDepth = 0;

                quoinRect.height = 1f / columnSegments;

                for (int row = 0; row < columnSegments; ++row)
                {
                    if (index % 2 == 0)
                    {
                        newQuads.Add(new Cell
                        {
                            Rect = new Rect(quoinRect.x, quoinRect.y, quoinRect.width * 0.5f, quoinRect.height),
                            Z = Z,
                            Depth = Depth,
                            Row = row,
                            Column = -1,
                            Border = new float4(Depth * 0.5f, 0, 0, 0),
                        });
                        CozyCut.Cut(new Rect(quoinRect.x, quoinRect.y, quoinRect.width * 0.5f + uOffsetByDepth, quoinRect.height), cells);
                    }
                    else
                    {
                        newQuads.Add(new Cell
                        {
                            Rect = new Rect(quoinRect.x + quoinRect.width * 0.5f, quoinRect.y, quoinRect.width * 0.5f, quoinRect.height),
                            Z = Z,
                            Depth = Depth,
                            Row = row,
                            Column = -1,
                            Border = new float4(0, Depth * 0.5f, 0, 0),
                        });
                        CozyCut.Cut(new Rect(quoinRect.x + quoinRect.width * 0.5f - uOffsetByDepth, quoinRect.y, quoinRect.width * 0.5f + uOffsetByDepth, quoinRect.height), cells);
                    }

                    quoinRect.y += 1f / columnSegments;

                    index++;
                }

                if (Mirror)
                    index++;
            }

            cells.AddRange(newQuads);
        }

        public uint CalculateHash()
        {
            HashBuffer hash = new HashBuffer();
            hash.Write(CornerAngle);
            hash.Write(Z);
            hash.Write(Width);
            hash.Write(Height);
            hash.Write(Depth);
            hash.Write(Mirror);
            return hash.GetHash();
        }

        void OnEnable() { }
    }
}