using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace ProjectDawn.CozyBuilder
{
    public unsafe ref struct HashBuffer
    {
        fixed byte Data[512];
        int Head;

        public void Write<T>(T value) where T : unmanaged
        {
            fixed (byte* ptr = Data)
            {
                UnsafeUtility.MemCpy(ptr + Head, &value, sizeof(T));
                Head += sizeof(T);
            }
        }

        public uint GetHash()
        {
            fixed (byte* ptr = Data)
            {
                return math.hash(ptr, Head);
            }
        }
    }
}