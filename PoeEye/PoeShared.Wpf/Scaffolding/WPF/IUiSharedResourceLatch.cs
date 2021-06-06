using System;
using System.ComponentModel;

namespace PoeShared.Scaffolding.WPF
{
    public interface IUiSharedResourceLatch : INotifyPropertyChanged
    {
        bool IsBusy { get; }
        
        IDisposable Rent();
    }
}