using System;

namespace PoeShared.Native;

internal sealed class WindowTrackerTitleMatcher : IWindowTrackerMatcher
{
    private readonly IStringMatcher titleMatcher;

    public WindowTrackerTitleMatcher(IStringMatcher titleMatcher)
    {
        this.titleMatcher = titleMatcher;
    }

    public bool IsMatch(IWindowHandle window)
    {
        return titleMatcher.IsMatch(window.Title);
    }
}