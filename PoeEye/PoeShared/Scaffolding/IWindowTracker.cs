using System;

namespace PoeShared.Scaffolding
{
    public interface IWindowTracker
    {
        bool IsActive { get; }

        IntPtr MatchingWindowHandle { get; }

        string ActiveWindowTitle { get; }

        IntPtr ActiveWindowHandle { get; }

        string TargetWindowName { get; }
    }
}