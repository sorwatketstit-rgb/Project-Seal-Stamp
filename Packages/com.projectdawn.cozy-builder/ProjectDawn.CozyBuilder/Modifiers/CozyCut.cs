using Unity.Collections;
using UnityEngine;

namespace ProjectDawn.CozyBuilder
{
    [AddComponentMenu("Cozy Builder/Modifier/Cozy Cut")]

    public class CozyCut : MonoBehaviour, ICozyGridModifier
    {
        public void Modify(ref CozyBuilderContext context, NativeList<Cell> cells, CozyPlane plane)
        {
            using (var masks = transform.parent?.GetComponentsInChildrenScoped<CozyMask>())
                foreach (var mask in masks?.List)
                {
                    Rect cut = plane.GetRect(mask.transform.position + (Vector3)mask.Offset, mask.Size);
                    Cut(cut, cells);
                }
        }

        public uint CalculateHash()
        {
            HashBuffer hash = new HashBuffer();
            using (var masks = transform.parent?.GetComponentsInChildrenScoped<CozyMask>())
                foreach (var mask in masks?.List)
                    hash.Write(mask.CalculateHash());
            return hash.GetHash();
        }

        public static void Cut(Rect cut, NativeList<Cell> cells)
        {
            int length = cells.Length;

            // Loop through each quad and determine how to modify it
            for (int i = 0; i < length; i++)
            {
                Cell cell = cells[i];
                Rect rect = cell.Rect;

                // If the cuttingQuad does not overlap with this quad, keep it as is
                if (!cut.Overlaps(rect))
                {
                    cells.Add(cell);
                    continue;
                }

                // Find the intersection of the cuttingQuad and the current quad
                Rect intersection = Rect.MinMaxRect(
                    Mathf.Max(rect.xMin, cut.xMin),
                    Mathf.Max(rect.yMin, cut.yMin),
                    Mathf.Min(rect.xMax, cut.xMax),
                    Mathf.Min(rect.yMax, cut.yMax)
                );

                // If the cuttingQuad is fully inside the current quad, create four smaller cells
                if (rect.Contains(cut.min) && rect.Contains(cut.max))
                {
                    // Create left quad
                    cells.Add(cell.Copy(new Rect(rect.xMin, rect.yMin, cut.xMin - rect.xMin, rect.height)));

                    // Create right quad
                    cells.Add(cell.Copy(new Rect(cut.xMax, rect.yMin, rect.xMax - cut.xMax, rect.height)));

                    // Create bottom quad
                    cells.Add(cell.Copy(new Rect(cut.xMin, rect.yMin, cut.width, cut.yMin - rect.yMin)));

                    // Create top quad
                    cells.Add(cell.Copy(new Rect(cut.xMin, cut.yMax, cut.width, rect.yMax - cut.yMax)));
                }
                else
                {
                    // If the cuttingQuad partially overlaps the current quad, we need to cut along the edges of the intersection

                    // Check for overlap on the left side
                    if (intersection.xMin > rect.xMin)
                    {
                        cells.Add(cell.Copy(new Rect(rect.xMin, rect.yMin, intersection.xMin - rect.xMin, rect.height)));
                    }

                    // Check for overlap on the right side
                    if (intersection.xMax < rect.xMax)
                    {
                        cells.Add(cell.Copy(new Rect(intersection.xMax, rect.yMin, rect.xMax - intersection.xMax, rect.height)));
                    }

                    // Check for overlap on the bottom side
                    if (intersection.yMin > rect.yMin)
                    {
                        cells.Add(cell.Copy(new Rect(intersection.xMin, rect.yMin, intersection.width, intersection.yMin - rect.yMin)));
                    }

                    // Check for overlap on the top side
                    if (intersection.yMax < rect.yMax)
                    {
                        cells.Add(cell.Copy(new Rect(intersection.xMin, intersection.yMax, intersection.width, rect.yMax - intersection.yMax)));
                    }
                }
            }

            cells.RemoveRange(0, length);
        }

        void OnEnable() { }
    }
}