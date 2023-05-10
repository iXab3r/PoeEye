using System.ComponentModel;

namespace PoeShared.Native;

public interface IOverlayCanBeLocked : INotifyPropertyChanged
{
    bool IsLocked { get; set; }

    bool IsUnlockable { get; }
}