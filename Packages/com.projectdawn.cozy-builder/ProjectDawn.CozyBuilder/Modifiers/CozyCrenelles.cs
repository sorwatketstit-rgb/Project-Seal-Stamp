using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace ProjectDawn.CozyBuilder
{
    /// <summary>
    /// Removes bricks at specific rows in a fixed pattern to create a medieval defensive wall look.
    /// </summary>
    [AddComponentMenu("Cozy Builder/Modifier/Cozy Crenelles")]
    public class CozyCrenelles : MonoBehaviour, ICozyGridModifier
    {
        [Tooltip("The frequency at which bricks are removed from the wall. Higher values mean fewer bricks are removed.")]
        [Range(2, 20)]
        public int Frequency = 2;

        [Tooltip("The specific row where bricks will be removed to create the pattern.")]
        public int Row = 0;

        [Tooltip("If enabled, inverts the pattern of brick removal.")]
        public bool Invert = true;
        public void Modify(ref CozyBuilderContext context, NativeList<Cell> cells, CozyPlane plane)
        {
            if (Invert)
            {
                int maxRow = 0;
                for (int i = 0; i < cells.Length; i++)
                    maxRow = math.max(maxRow, cells[i].Row);

                for (int i = 0; i < cells.Length; i++)
                {
                    Cell quad = cells[i];
                    if (quad.Row != maxRow - Row)
                        continue;

                    if (quad.Column % Frequency != 0)
                        continue;

                    cells.RemoveAt(i);
                }
            }
            else
            {
                for (int i = 0; i < cells.Length; i++)
                {
                    Cell quad = cells[i];
                    if (quad.Row != Row)
                        continue;

                    if (quad.Column % Frequency != 0)
                        continue;

                    cells.RemoveAt(i);
                }
            }
        }

        public uint CalculateHash()
        {
            HashBuffer hash = new HashBuffer();
            hash.Write(Frequency);
            hash.Write(Row);
            hash.Write(Invert);
            return hash.GetHash();
        }

        void OnEnable() { }
    }
}