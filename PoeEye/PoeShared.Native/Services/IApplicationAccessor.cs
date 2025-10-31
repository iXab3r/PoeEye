using System;
using System.ComponentModel;
using System.Reactive;
using System.Windows;

namespace PoeShared.Services;

public interface IApplicationAccessor : INotifyPropertyChanged
{
    /// <summary>
    ///   Happens only when Exit() is called, Terminate bypasses this
    /// </summary>
    IObservable<int> WhenExit { get; }
    
    IObservable<int> WhenTerminate { get; }
    
    IObservable<Unit> WhenLoaded { get; }

    void Exit();

    /// <summary>
    ///  Terminates instantly
    /// </summary>
    void Terminate(int exitCode);
        
    bool IsExiting { get; }
    
    bool IsElevated { get; }
        
    /// <summary>
    ///  IsLoaded is set to true after main window is loaded
    /// </summary>
    bool IsLoaded { get; }
        
    bool LastExitWasGraceful { get; }
        
    bool LastLoadWasSuccessful { get; }
    
    Window MainWindow { get; }
    
    void ReportIsLoaded();

    void ReplaceExecutable(string processPath, string arguments = default);
    
    void RestartAs(string processPath, string arguments = default, string verb = default);

    void RestartAsAdmin();
}