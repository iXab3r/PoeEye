using System;

namespace PoeShared.Scaffolding
{
    public static class IntPtrExtensions
    {
        public static string ToHexadecimal(this IntPtr value)
        {
            return $"0x{value.ToInt64():x8}";
        }
    }
}