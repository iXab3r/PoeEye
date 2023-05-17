using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.Native;

internal sealed class TrackedOverlayWindowController : OverlayWindowController, ITrackedOverlayWindowController
{
    public TrackedOverlayWindowController(
        IWindowTracker windowTracker, 
        [Unity.Dependency(WellKnownDispatchers.UIOverlay)] IScheduler overlayScheduler) : base(overlayScheduler)
    {
        Log.Info(() => $"Creating overlay window controller using {windowTracker}");
        WindowTracker = windowTracker;
        
        Observable.Merge(windowTracker.WhenAnyValue(x => x.ActiveWindowHandle).ToUnit(), this.WhenAnyValue(x => x.IsEnabled).ToUnit())
            .Select(
                x => new
                {
                    ActiveTitle = windowTracker.ActiveWindowTitle,
                    WindowIsActive = windowTracker.IsActive,
                    OverlayIsActive = IsPairedOverlay(windowTracker.ActiveWindowHandle),
                    IsEnabled
                })
            .Do(x => Log.Debug(() => $"Active window has changed: {x}"))
            .Select(x => (x.WindowIsActive || x.OverlayIsActive) && IsEnabled)
            .DistinctUntilChanged()
            .Do(x => Log.Debug(() => $"Sending SetVisibility({x}) to window scheduler"))
            .ObserveOn(overlayScheduler)
            .SubscribeSafe(SetVisibility, Log.HandleUiException)
            .AddTo(Anchors);
        
        windowTracker
            .WhenAnyValue(x => x.MatchingWindow)
            .Where(x => x != null && !IsPairedOverlay(windowTracker.ActiveWindowHandle))
            .SubscribeSafe(x => LastActiveWindow = x, Log.HandleUiException)
            .AddTo(Anchors);
    }

    public IWindowHandle LastActiveWindow { get; private set; }

    public bool IsEnabled { get; set; }
    
    private bool IsPairedOverlay(IntPtr hwnd)
    {
        return Children.Items.Any(x => x.WindowController != null && x.WindowController.Handle == hwnd);
    }

    public IWindowTracker WindowTracker { get; }

    public void ActivateLastActiveWindow()
    {
        var windowHandle = LastActiveWindow;
        if (windowHandle == default)
        {
            return;
        }

        UnsafeNative.SetForegroundWindow(windowHandle);
    }
}