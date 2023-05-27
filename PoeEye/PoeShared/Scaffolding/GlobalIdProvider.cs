namespace PoeShared.Scaffolding;

public sealed class GlobalIdProvider
{
    private static long globalIdx;

    public string Next(string prefix = null)
    {
        return $"{prefix}{Interlocked.Increment(ref globalIdx)}";
    }
}