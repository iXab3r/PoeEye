using System.ComponentModel;

namespace PoeShared.Common;

public interface IPauseController : INotifyPropertyChanged
{
    bool IsPaused { get; }

    IDisposable Pause();
}