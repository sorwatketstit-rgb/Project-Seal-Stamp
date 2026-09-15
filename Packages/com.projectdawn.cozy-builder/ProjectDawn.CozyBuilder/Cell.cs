using Unity.Mathematics;
using UnityEngine;

namespace ProjectDawn.CozyBuilder
{
    public struct Segment
    {
        public float X;
        public float Width;
        public float Z;
        public float Depth;
        public int Index;
    }

    public struct Cell
    {
        public Rect Rect;
        public float Z;
        public float Depth;
        public int Row;
        public int Column;
        public float4 Border;

        public Cell(Rect rect, int row, int column)
        {
            Rect = rect;
            Row = row;
            Column = column;
            Z = 0;
            Depth = 1;
            Border = 0;
        }

        public Cell(Rect rect, float z, float depth, int row, int column)
        {
            Rect = rect;
            Row = row;
            Column = column;
            Z = z;
            Depth = depth;
            Border = 0;

        }

        public Cell Copy(Rect rect)
        {
            Cell copy = this;
            copy.Border = 0;
            copy.Rect = rect;
            return copy;
        }
    }
}