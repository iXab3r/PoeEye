using System;
using System.Reactive;

namespace PoeShared.Native
{
    public interface IWindowViewController : IViewController
    {
        IObservable<Unit> WhenLoaded { get; }
        
        IObservable<Unit> WhenUnloaded { get; }
        
        IObservable<Unit> WhenRendered { get; }
    }
}