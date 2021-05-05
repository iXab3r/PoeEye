namespace PoeShared.Scaffolding
{
    public static class BinaryExtensions
    {
        public static byte GetBits(this byte value, byte start, byte length)
        {
            return (byte)GetBits((ulong) value, start, length);
        }
        
        public static int GetBits(this int value, byte start, byte length)
        {
            return (int)GetBits((ulong) value, start, length);
        }
        
        public static uint GetBits(this uint value, byte start, byte length)
        {
            return (uint)GetBits((ulong) value, start, length);
        }
        
        public static ulong GetBits(this ulong value, byte start, byte length)
        {
            if (length <= 0)
            {
                return 0;
            }
            const int bitCount = sizeof(ulong) * 8;
            var shl = bitCount - start - length;
            var shr = bitCount - length;
            return value << shl >> shr;
        }
    }
}