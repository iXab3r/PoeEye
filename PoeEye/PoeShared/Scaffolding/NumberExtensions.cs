using System;

namespace PoeShared.Scaffolding
{
    public static class NumberExtensions
    {
        public static string ToHexadecimal(this long value)
        {
            return $"0x{value:x8}";
        }
        
        public static string ToHexadecimal(this int value)
        {
            return ToHexadecimal((long)value);
        }
        
        public static string ToHexadecimal(this uint value)
        {
            return ToHexadecimal((long)value);
        }
        
        public static string ToHexadecimal(this ushort value)
        {
            return ToHexadecimal((long)value);
        }
        
        public static TimeSpan EnsureInRange(this TimeSpan value, TimeSpan min, TimeSpan max)
        {
            return TimeSpan.FromMilliseconds(EnsureInRange(value.TotalMilliseconds, min.TotalMilliseconds,
                max.TotalMilliseconds));
        }

        public static double EnsureInRange(this double value, double min, double max)
        {
            return Math.Max(Math.Min(value, max), min);
        }
    }
}