using System;
using System.ComponentModel;
using JetBrains.Annotations;

namespace PoeShared.Native;

public interface IForegroundWindowTracker : INotifyPropertyChanged
{
    IWindowHandle ForegroundWindow { get; }
}