using System;
using System.ComponentModel;
using JetBrains.Annotations;

namespace PoeShared.Native;

public interface IWindowTracker : INotifyPropertyChanged
{
    bool IsActive { get; }

    IntPtr MatchingWindowHandle { get; }
    
    IWindowHandle MatchingWindow { get; }

    string ActiveWindowTitle { [CanBeNull] get; }

    IntPtr ActiveWindowHandle { get; }
        
    IWindowHandle ActiveWindow { get; }
        
    int ActiveProcessId { get; }
        
    int ExecutingProcessId { get; }

    string Name { get; }
}