using System.Threading;

namespace PoeShared.Blazor.Controls;

internal static class TreeViewHelper
{
    private static long nodeId;

    public static long GetNextNodeId()
    {
        return Interlocked.Increment(ref nodeId);
    }
}