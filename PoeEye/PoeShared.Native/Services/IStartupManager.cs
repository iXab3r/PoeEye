using System.ComponentModel;

namespace PoeShared.Services;

public interface IStartupManager : INotifyPropertyChanged
{
    bool IsRegistered { get; }
        
    bool Register();
        
    bool Unregister();
}