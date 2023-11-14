using System.ComponentModel;

namespace PoeShared.Services;

public interface ISafeModeService : INotifyPropertyChanged
{
    bool IsInSafeMode { get; }
    void ExitSafeMode();
    void EnterSafeMode();
}