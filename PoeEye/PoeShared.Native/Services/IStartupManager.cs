using System.ComponentModel;

namespace PoeShared.Services;

public interface IStartupManager : INotifyPropertyChanged
{
    bool IsReady { get; }
    
    bool IsRegistered { get; }
        
    bool Register();
        
    bool Unregister();
}