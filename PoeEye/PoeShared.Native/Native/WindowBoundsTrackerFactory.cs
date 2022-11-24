using System;
using System.Drawing;
using System.Reactive.Linq;
using PInvoke;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.Native;

internal sealed class WindowBoundsTrackerFactory : IWindowBoundsTrackerFactory
{
    private readonly IFactory<IWinEventHookWrapper, WinEventHookArguments> hookFactory;

    public WindowBoundsTrackerFactory(IFactory<IWinEventHookWrapper, WinEventHookArguments> hookFactory)
    {
        this.hookFactory = hookFactory;
    }

    public IObservable<Rectangle?> Track(IWindowHandle windowToTrack)
    {
        var locationChangeHook = hookFactory.Create(new WinEventHookArguments()
        {
            Flags = User32.WindowsEventHookFlags.WINEVENT_OUTOFCONTEXT,
            EventMin = User32.WindowsEventHookType.EVENT_OBJECT_LOCATIONCHANGE,
            EventMax = User32.WindowsEventHookType.EVENT_OBJECT_LOCATIONCHANGE,
        });

        return locationChangeHook.WhenWindowEventTriggered.Where(y => y.WindowHandle == windowToTrack.Handle)
            .StartWithDefault()
            .Select(x => (Rectangle?)windowToTrack.WindowBounds)
            .DistinctUntilChanged();
    }

    public IWindowBoundsTracker CreateTracker()
    {
        return new WindowBoundsTracker(this);
    }

    private sealed class WindowBoundsTracker : DisposableReactiveObject, IWindowBoundsTracker
    {
        private readonly WindowBoundsTrackerFactory owner;

        public WindowBoundsTracker(WindowBoundsTrackerFactory owner)
        {
            this.owner = owner;
            
            this.WhenAnyValue(x => x.Window)
                .Select(x =>
                    x != null
                        ? owner.Track(x)
                        : Observable.Return(default(Rectangle?)))
                .Switch()
                .DistinctUntilChanged()
                .Subscribe(x => Bounds = x)
                .AddTo(Anchors);
        }

        public IWindowHandle Window { get; set; }
        public Rectangle? Bounds { get; private set; }
    }
}