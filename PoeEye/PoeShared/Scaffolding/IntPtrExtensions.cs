namespace PoeShared.Scaffolding;

public static class IntPtrExtensions
{
    public static string ToHexadecimal(this IntPtr value)
    {
        var handle = value.ToInt64();
        return handle.ToHexadecimal();
    }

    public static ushort LoWord(this IntPtr value)
    {
        return (ushort)(value.ToInt64() & 0xFFFF);
    }

    public static ushort HiWord(this IntPtr value)
    {
        return (ushort)((value.ToInt64() >> 16) & 0xFFFF);
    }
}