using System;
using System.ComponentModel;
using System.Reactive;
using System.Windows;

namespace PoeShared.Native;

public interface IWindowViewController : INotifyPropertyChanged, IViewController
{
    IObservable<Unit> WhenLoaded { get; }
        
    IObservable<Unit> WhenUnloaded { get; }

    IObservable<Unit> WhenClosed { get; }
        
    IObservable<CancelEventArgs> WhenClosing { get; }
        
    IObservable<Unit> WhenRendered { get; }
        
    IntPtr Handle { get; }

    Window Window { get; }
        
    void TakeScreenshot(string fileName);
        
    void Minimize();

    void Activate();

    void Close(bool? result);
        
    void Close();
        
    bool Topmost { get; set; }
}