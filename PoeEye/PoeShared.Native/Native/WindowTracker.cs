using System;
using System.Reactive.Linq;
using PInvoke;
using PoeShared.Logging;
using PoeShared.Prism;
using PoeShared.Scaffolding;

namespace PoeShared.Native;

public class WindowTracker : DisposableReactiveObject, IWindowTracker
{
    private static readonly IFluentLog Log = typeof(WindowTracker).PrepareLogger();
    private static readonly TimeSpan RecheckPeriod = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan SamplePeriod = TimeSpan.FromMilliseconds(100);
    private readonly IWindowTrackerMatcher windowMatcher;
    private readonly IWindowHandleProvider windowHandleProvider;

    public WindowTracker(
        IFactory<IWinEventHookWrapper, WinEventHookArguments> hookFactory,
        IWindowTrackerMatcher windowMatcher,
        IWindowHandleProvider windowHandleProvider)
    {
        Guard.ArgumentNotNull(windowMatcher, nameof(windowMatcher));

        this.windowMatcher = windowMatcher;
        this.windowHandleProvider = windowHandleProvider;

        var timerObservable = Observables
            .BlockingTimer(RecheckPeriod, timerName: "WndTracker")
            .ToUnit();

        var objectFocusHook = hookFactory.Create(new WinEventHookArguments
        {
            Flags = User32.WindowsEventHookFlags.WINEVENT_OUTOFCONTEXT,
            EventMin = User32.WindowsEventHookType.EVENT_OBJECT_FOCUS,
            EventMax = User32.WindowsEventHookType.EVENT_OBJECT_FOCUS,
        });

        Observable.Merge(timerObservable
                    .Select(_ => new
                    {
                        Reason = "Timer",
                        ForegroundWindow = UnsafeNative.GetForegroundWindow()
                    }),
                objectFocusHook.WhenWindowEventTriggered.Select(x => new
                {
                    Reason = nameof(User32.WindowsEventHookType.EVENT_OBJECT_FOCUS),
                    ForegroundWindow = x.WindowHandle
                }))
            .Select(x => new
            {
                WindowHandle = windowHandleProvider.GetByWindowHandle(x.ForegroundWindow),
                x.Reason
            })
            .DistinctUntilChanged(x => x.WindowHandle)
            .SubscribeSafe(x => WindowActivated(x.WindowHandle), Log.HandleUiException)
            .AddTo(Anchors);
    }

    public int ExecutingProcessId => Environment.ProcessId;

    public string Name { get; set; }

    public bool IsActive { get; private set; }

    public IntPtr MatchingWindowHandle => MatchingWindow?.Handle ?? IntPtr.Zero;

    public string ActiveWindowTitle => ActiveWindow?.Title;

    public IntPtr ActiveWindowHandle => ActiveWindow?.Handle ?? IntPtr.Zero;

    public IWindowHandle ActiveWindow { get; private set; }

    public IWindowHandle MatchingWindow { get; private set; }

    public int ActiveProcessId => ActiveWindow?.ProcessId ?? default;

    protected override void FormatToString(ToStringBuilder builder)
    {
        base.FormatToString(builder);
        builder.AppendParameter(nameof(Name), Name);
    }

    private void WindowActivated(IWindowHandle window)
    {
        var previousState = new {IsActive, MatchingWindowHandle, ActiveWindowTitle, ActiveWindowHandle, ActiveProcessId};
        ActiveWindow = window;
        IsActive = windowMatcher.IsMatch(window);
        MatchingWindow = IsActive ? ActiveWindow : default;

        if (previousState.ActiveWindowHandle != ActiveWindowHandle)
        {
            Log.Debug(() => $"[#{Name}] Target window is {(IsActive ? string.Empty : "NOT ")}ACTIVE ({window.Handle.ToHexadecimal()}, title '{ActiveWindowTitle}')");
        }

        this.RaiseIfChanged(nameof(IsActive), previousState.IsActive, IsActive);
        this.RaiseIfChanged(nameof(MatchingWindowHandle), previousState.MatchingWindowHandle, MatchingWindowHandle);
        this.RaiseIfChanged(nameof(ActiveWindowTitle), previousState.ActiveWindowTitle, ActiveWindowTitle);
        this.RaiseIfChanged(nameof(ActiveWindowHandle), previousState.ActiveWindowHandle, ActiveWindowHandle);
        this.RaiseIfChanged(nameof(ActiveProcessId), previousState.ActiveProcessId, ActiveProcessId);
    }
}