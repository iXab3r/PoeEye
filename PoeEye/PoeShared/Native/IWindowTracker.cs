using System;
using System.ComponentModel;
using JetBrains.Annotations;

namespace PoeShared.Native
{
    public interface IWindowTracker : INotifyPropertyChanged
    {
        bool IsActive { get; }

        IntPtr MatchingWindowHandle { get; }

        string ActiveWindowTitle { [CanBeNull] get; }

        IntPtr ActiveWindowHandle { get; }

        string Name { get; }
    }
}