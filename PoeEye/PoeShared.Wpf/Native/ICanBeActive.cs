using System.ComponentModel;

namespace PoeShared.Native;

public interface ICanBeActive : INotifyPropertyChanged
{
    bool IsActive { get; }
}