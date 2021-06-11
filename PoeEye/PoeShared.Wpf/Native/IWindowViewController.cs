using System;
using System.ComponentModel;
using System.Reactive;

namespace PoeShared.Native
{
    public interface IWindowViewController : INotifyPropertyChanged, IViewController
    {
        IObservable<Unit> WhenLoaded { get; }
        
        IObservable<Unit> WhenUnloaded { get; }

        IObservable<Unit> WhenClosed { get; }
        
        IObservable<CancelEventArgs> WhenClosing { get; }
        
        IObservable<Unit> WhenRendered { get; }
        
        IntPtr Handle { get; }

        void TakeScreenshot(string fileName);
        
        void Minimize();

        void Close();
        
        bool Topmost { get; set; }
    }
}