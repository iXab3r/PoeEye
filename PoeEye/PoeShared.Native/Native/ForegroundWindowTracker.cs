using System;
using System.Reactive.Linq;
using PInvoke;
using PoeShared.Prism;
using PoeShared.Scaffolding;

namespace PoeShared.Native;

internal sealed class ForegroundWindowTracker : DisposableReactiveObjectWithLogger, IForegroundWindowTracker
{
    private static readonly TimeSpan RecheckPeriod = TimeSpan.FromMilliseconds(250);

    public ForegroundWindowTracker(
        IWindowHandleProvider windowHandleProvider,
        IFactory<IWinEventHookWrapper, WinEventHookArguments> hookFactory)
    {
        var timerObservable = Observables
            .BlockingTimer(RecheckPeriod, timerName: "WndTracker")
            .Select(_ => new
            {
                Reason = "Timer",
            });

        var objectFocusHook = Observable.Merge(
                hookFactory.Create(new WinEventHookArguments
                    {
                        Flags = User32.WindowsEventHookFlags.WINEVENT_OUTOFCONTEXT,
                        EventMin = User32.WindowsEventHookType.EVENT_OBJECT_CREATE,
                        EventMax = User32.WindowsEventHookType.EVENT_OBJECT_FOCUS,
                    })
                    .WhenWindowEventTriggered,
                hookFactory.Create(new WinEventHookArguments
                    {
                        Flags = User32.WindowsEventHookFlags.WINEVENT_OUTOFCONTEXT,
                        EventMin = User32.WindowsEventHookType.EVENT_OBJECT_NAMECHANGE,
                        EventMax = User32.WindowsEventHookType.EVENT_OBJECT_DESCRIPTIONCHANGE,
                    })
                    .WhenWindowEventTriggered
            ).Select(x => new
            {
                Reason = $"Event: {x.EventId}",
            })
            .Select(x => x);

        // the idea is to start by tracking events, but do periodic checks afterwards until the next event arrives
        var updateObservable = objectFocusHook.Select(x => Observable.Return(x)).Switch();

        updateObservable
            .StartWith(new { Reason = "Initial tick" })
            .Select(x => new { x.Reason, ForegroundWindow = UnsafeNative.GetForegroundWindow() })
            .DistinctUntilChanged(x => x.ForegroundWindow)
            .Select(x => new
            {
                WindowHandle = windowHandleProvider.GetByWindowHandle(x.ForegroundWindow),
                x.Reason
            })
            .SubscribeSafe(x => ForegroundWindow = x.WindowHandle, Log.HandleUiException)
            .AddTo(Anchors);
    }

    public IWindowHandle ForegroundWindow { get; private set; }
}