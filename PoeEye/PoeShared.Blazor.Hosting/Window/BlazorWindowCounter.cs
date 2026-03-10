using System.Threading;

namespace PoeShared.Blazor.Wpf;

internal static class BlazorWindowCounter
{
    private static long counter;

    public static long GetNext()
    {
        return Interlocked.Increment(ref counter);
    }
}