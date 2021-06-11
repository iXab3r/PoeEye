using System;
using System.ComponentModel;

namespace PoeShared.Scaffolding.WPF
{
    public interface IUiSharedResourceLatch : INotifyPropertyChanged
    {
        bool IsBusy { get; }
        
        bool IsPaused { get; }
        
        IDisposable Rent();

        IDisposable Pause();
    }
}