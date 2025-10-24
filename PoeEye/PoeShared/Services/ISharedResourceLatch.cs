#nullable enable
using System.ComponentModel;

namespace PoeShared.Services;

public interface ISharedResourceLatch : IDisposable, INotifyPropertyChanged
{
    bool IsBusy { get; }
        
    string? Name { get; }

    IDisposable Rent();
}