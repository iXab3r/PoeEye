using System;
using JetBrains.Annotations;

namespace PoeShared.Native
{
    public interface IWindowTracker
    {
        bool IsActive { get; }

        IntPtr MatchingWindowHandle { get; }

        string ActiveWindowTitle { [CanBeNull] get; }

        IntPtr ActiveWindowHandle { get; }
    }
}