using System;
using System.Reactive;

namespace PoeShared.Native
{
    public interface IViewController
    {
        IObservable<Unit> WhenLoaded { get; }
    }
}