using System;
using System.ComponentModel;

namespace PoeShared.Scaffolding.WPF;

public interface IBusyController : INotifyPropertyChanged
{
    bool IsBusy { get; }

    IDisposable Busy();
}