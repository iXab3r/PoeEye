namespace PoeShared.Scaffolding;

public static class NumberExtensions
{
    public static string ToHexadecimal(this long value)
    {
        return $"0x{value:X8}";
    }
    
    public static string ToHexadecimal(this ulong value)
    {
        return $"0x{value:X8}";
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
        
    public static bool IsInRange(this TimeSpan value, TimeSpan min, TimeSpan max)
    {
        return value >= min && value <= max;
    }

    public static int EnsureInRange(this int value, int min, int max)
    {
        return Math.Max(Math.Min(value, max), min);
    }
    
    public static long EnsureInRange(this long value, long min, long max)
    {
        return Math.Max(Math.Min(value, max), min);
    }

    public static int EnsureOdd(this int value)
    {
        if (value % 2 != 0)
        {
            return value;
        }

        return value - 1;
    }
     
    public static double EnsureInRange(this double value, double min, double max)
    {
        return Math.Max(Math.Min(value, max), min);
    }
    
    public static float EnsureInRange(this float value, float min, float max)
    {
        return Math.Max(Math.Min(value, max), min);
    }
}