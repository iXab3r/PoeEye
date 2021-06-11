using System;
using System.Reactive;
using System.Windows;

namespace PoeShared.Services
{
    public interface IApplicationAccessor
    {
        IObservable<Unit> WhenExit { get; }
    }
}