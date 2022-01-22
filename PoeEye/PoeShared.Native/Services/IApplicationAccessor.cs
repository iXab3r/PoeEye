using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace PoeShared.Services;

public interface IApplicationAccessor : INotifyPropertyChanged
{
    IObservable<int> WhenExit { get; }

    Task Exit();

    /// <summary>
    ///  Terminates instantly
    /// </summary>
    void Terminate(int exitCode);
        
    bool IsExiting { get; }
        
    /// <summary>
    ///  IsLoaded is set to true after main window is loaded
    /// </summary>
    bool IsLoaded { get; }
        
    bool LastExitWasGraceful { get; }
        
    bool LastLoadWasSuccessful { get; }

    void ReportIsLoaded();
}