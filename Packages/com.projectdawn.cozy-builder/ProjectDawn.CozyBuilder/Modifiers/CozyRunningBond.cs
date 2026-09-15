using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace ProjectDawn.CozyBuilder
{
    public enum BondOrentation
    {
        Horizontal,
        Vertical,
    }

    /// <summary>
    /// Running Bond rearranges cell widths to create a brick-like pattern and occasionally splits bricks.
    /// </summary>
    [AddComponentMenu("Cozy Builder/Modifier/Cozy Running Bond")]
    public class CozyRunningBond : MonoBehaviour, ICozyGridModifier
    {
        [Tooltip("Seed value for generating random scales and patterns to ensure repeatable results.")]
        public uint Seed = 1;

        [Tooltip("The range for scaling the width of cells.")]
        [MinMaxRange(0, 2)]
        public Range WidthScale = new Range(0.5f, 1.1f);

        [Tooltip("The range for scaling the depth of cells.")]
        [MinMaxRange(0, 2)]
        public Range DepthScale = new Range(0.9f, 1.2f);

        [Tooltip("Specifies the orientation of the bond pattern (Horizontal or Vertical).")]
        public BondOrentation Orentation = BondOrentation.Horizontal;

        [Tooltip("The minimum height for splitting cells. Prevents creating excessively small cells.")]
        public float MinHeight = 0.03f;

        public void Modify(ref CozyBuilderContext context, NativeList<Cell> cells, CozyPlane plane)
        {
            Random random = new Random(Seed);

            if (Orentation == BondOrentation.Vertical)
            {
                int column = 0;
                while (true)
                {
                    List<(float, int)> rowSegmentLengths = new();
                    float length = 0;
                    for (int i = 0; i < cells.Length; i++)
                    {
                        Cell quad = cells[i];

                        if (quad.Column == column)
                        {
                            float newWidth = quad.Rect.height * random.NextFloat(WidthScale.Start, WidthScale.End);
                            rowSegmentLengths.Add((newWidth, i));
                            length += newWidth;
                        }
                    }

                    float offset = 0;
                    for (int i = 0; i < rowSegmentLengths.Count; i++)
                    {
                        (float newWidth, int quadIndex) = rowSegmentLengths[i];

                        newWidth /= length;

                        ref Cell quad = ref cells.ElementAt(quadIndex);

                        float diff = newWidth - quad.Rect.height;

                        quad.Rect.height = newWidth;
                        quad.Rect.y += offset;

                        offset += diff;
                    }

                    if (rowSegmentLengths.Count == 0)
                        break;

                    column++;
                }

                /*int count = cells.Length;
                for (int i = 0; i < count; i++)
                {
                    if (random.NextInt(0, 4) != 0)
                        continue;

                    Quad quad = cells[i];

                    if (quad.Rect.width < MinHeight * 2)
                        continue;

                    float split = random.NextFloat(MinHeight, quad.Rect.width - MinHeight);

                    Rect a = new Rect(quad.Rect.x, quad.Rect.y, split, quad.Rect.height);
                    Rect b = new Rect(quad.Rect.x, quad.Rect.y + split, quad.Rect.height - split, quad.Rect.height);

                    cells[i] = quad.Copy(a);
                    cells.Add(quad.Copy(b));
                }*/

                for (int i = 0; i < cells.Length; i++)
                {
                    ref Cell quad = ref cells.ElementAt(i);
                    float newDepth = quad.Depth * random.NextFloat(DepthScale.Start, DepthScale.End);
                    quad.Depth = newDepth;
                }
            }
            else
            {

                int column = 0;
                while (true)
                {
                    List<(float, int)> rowSegmentLengths = new();
                    float length = 0;
                    for (int i = 0; i < cells.Length; i++)
                    {
                        Cell quad = cells[i];

                        if (quad.Row == column)
                        {
                            float newWidth = quad.Rect.width * random.NextFloat(WidthScale.Start, WidthScale.End);
                            rowSegmentLengths.Add((newWidth, i));
                            length += newWidth;
                        }
                    }

                    float offset = 0;
                    for (int i = 0; i < rowSegmentLengths.Count; i++)
                    {
                        (float newWidth, int quadIndex) = rowSegmentLengths[i];

                        newWidth /= length;

                        ref Cell quad = ref cells.ElementAt(quadIndex);

                        float diff = newWidth - quad.Rect.width;

                        quad.Rect.width = newWidth;
                        quad.Rect.x += offset;

                        offset += diff;
                    }

                    if (rowSegmentLengths.Count == 0)
                        break;

                    column++;
                }

                int count = cells.Length;
                for (int i = 0; i < count; i++)
                {
                    if (random.NextInt(0, 4) != 0)
                        continue;

                    Cell quad = cells[i];

                    if (quad.Rect.height < MinHeight * 2)
                        continue;

                    float split = random.NextFloat(MinHeight, quad.Rect.height - MinHeight);

                    Rect a = new Rect(quad.Rect.x, quad.Rect.y, quad.Rect.width, split);
                    Rect b = new Rect(quad.Rect.x, quad.Rect.y + split, quad.Rect.width, quad.Rect.height - split);

                    cells[i] = quad.Copy(a);
                    cells.Add(quad.Copy(b));
                }

                for (int i = 0; i < cells.Length; i++)
                {
                    ref Cell quad = ref cells.ElementAt(i);
                    float newDepth = quad.Depth * random.NextFloat(DepthScale.Start, DepthScale.End);
                    quad.Depth = newDepth;
                }
            }
        }

        public uint CalculateHash()
        {
            HashBuffer hash = new HashBuffer();
            hash.Write(Seed);
            hash.Write(WidthScale);
            hash.Write(DepthScale);
            hash.Write(Orentation);
            return hash.GetHash();
        }

        void OnEnable() { }
    }
}