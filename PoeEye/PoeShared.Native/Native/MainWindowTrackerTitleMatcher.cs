using System;
using System.Diagnostics;
using PoeShared.Scaffolding;

namespace PoeShared.Native;

internal sealed class MainWindowTrackerTitleMatcher : IWindowTrackerMatcher
{
    public bool IsMatch(IWindowHandle window)
    {
        return window.IsOwnWindow();
    }
}