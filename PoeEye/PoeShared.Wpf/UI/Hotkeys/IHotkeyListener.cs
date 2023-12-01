using System;
using System.Reactive;
using PoeShared.Scaffolding;

namespace PoeShared.UI;

public interface IHotkeyListener : IDisposableReactiveObject
{
    IObservable<Unit> WhenActivated { get; }
}