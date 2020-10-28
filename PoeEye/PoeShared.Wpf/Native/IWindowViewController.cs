using System;
using System.Reactive;

namespace PoeShared.Native
{
    public interface IWindowViewController : IViewController
    {
        IObservable<Unit> WhenLoaded { get; }
        
        IObservable<Unit> WhenUnloaded { get; }

        IObservable<Unit> WhenClosed { get; }
        
        IObservable<Unit> WhenRendered { get; }
        
        void Minimize();
        
        bool Topmost { get; set; }
    }
}