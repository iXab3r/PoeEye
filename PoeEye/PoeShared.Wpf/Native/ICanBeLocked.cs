using System.ComponentModel;

namespace PoeShared.Native;

public interface ICanBeLocked : INotifyPropertyChanged
{
    bool IsLocked { get; set; }

    bool IsUnlockable { get; }
}