using System;
using System.ComponentModel;
using System.Reactive;
using System.Windows;
using System.Windows.Input;
using PoeShared.Scaffolding;
using PoeShared.UI;

namespace PoeShared.Native;

public interface IWindowViewController : IDisposableReactiveObject, IViewController
{
    IObservable<Unit> WhenLoaded { get; }
        
    IObservable<Unit> WhenUnloaded { get; }

    IObservable<Unit> WhenClosed { get; }
        
    IObservable<CancelEventArgs> WhenClosing { get; }
        
    IObservable<Unit> WhenRendered { get; }
    
    IObservable<KeyEventArgs> WhenKeyUp { get; }

    IObservable<KeyEventArgs> WhenKeyDown { get; }
    
    IObservable<KeyEventArgs> WhenPreviewKeyDown { get; }
    
    IObservable<KeyEventArgs> WhenPreviewKeyUp { get; }
    
    IntPtr Handle { get; }

    ReactiveMetroWindow Window { get; }
        
    void TakeScreenshot(string fileName);
        
    void Minimize();

    void Activate();

    void Close(bool? result);
        
    void Close();
        
    bool Topmost { get; set; }
}