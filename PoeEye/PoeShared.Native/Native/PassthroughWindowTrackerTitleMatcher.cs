using System;

namespace PoeShared.Native;

internal sealed class PassthroughWindowTrackerTitleMatcher : IWindowTrackerMatcher
{
    public bool IsMatch(IWindowHandle windowHandle)
    {
        return true;
    }
}