using System;
using System.Reactive.Concurrency;
using WindowsHook;

namespace PoeShared.Native;

internal interface IKeyboardMouseEventsProvider
{
    IObservable<IKeyboardMouseEvents> System { get; }
    IObservable<IKeyboardMouseEvents> Application { get; }
}