using System;
using System.ComponentModel;

namespace PoeShared.Scaffolding.WPF
{
    public interface IPauseController : INotifyPropertyChanged
    {
        bool IsPaused { get; }

        IDisposable Pause();
    }
}