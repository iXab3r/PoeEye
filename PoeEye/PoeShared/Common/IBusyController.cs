using System.ComponentModel;

namespace PoeShared.Common;

public interface IBusyController : INotifyPropertyChanged
{
    bool IsBusy { get; }

    IDisposable Busy();
}