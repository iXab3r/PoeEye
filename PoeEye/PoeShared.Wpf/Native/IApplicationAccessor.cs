using System;
using System.Reactive;

namespace PoeShared.Services
{
    public interface IApplicationAccessor
    {
        IObservable<Unit> WhenExit { get; }
    }
}