using System;

namespace PoeShared.Scaffolding
{
    public static class IntPtrExtensions
    {
        public static string ToHexadecimal(this IntPtr value)
        {
            var handle = value.ToInt64();
            return $"0x{handle:x8} ({value})";
        }
    }
}