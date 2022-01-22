using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Text;
using System.Windows.Forms;
using JetBrains.Annotations;
using log4net;
using PInvoke;
using PoeShared.Logging;
using PoeShared.Prism;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using ReactiveUI;
using ObservableEx = PoeShared.Scaffolding.ObservableEx;

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

        var timerObservable = ObservableEx
            .BlockingTimer(RecheckPeriod, timerName: "WndTracker")
            .ToUnit();
            
        var objectFocusHook = hookFactory.Create(new WinEventHookArguments
        {
            Flags = User32.WindowsEventHookFlags.WINEVENT_OUTOFCONTEXT,
            EventMin = User32.WindowsEventHookType.EVENT_OBJECT_FOCUS,
            EventMax = User32.WindowsEventHookType.EVENT_OBJECT_FOCUS,
        });

        timerObservable
            .Select(_ => new { Reason = "Timer", ForegroundWindow = UnsafeNative.GetForegroundWindow() })
            .Merge(objectFocusHook.WhenWindowEventTriggered.Select(x => new { Reason =  nameof( User32.WindowsEventHookType.EVENT_OBJECT_FOCUS), ForegroundWindow = x.WindowHandle }))
            .Select(x => new { ActiveWindow = x.ForegroundWindow, Title = UnsafeNative.GetWindowTitle(x.ForegroundWindow), ProcessId = UnsafeNative.GetProcessIdByWindowHandle(x.ForegroundWindow), Reason = x.Reason })
            .DistinctUntilChanged()
            .SubscribeSafe(x => WindowActivated(x.ActiveWindow, x.Title, x.ProcessId), Log.HandleUiException)
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

    public override string ToString()
    {
        return $"#Tracker{Name}";
    }

    private void WindowActivated(IntPtr hwnd, string title, int processId)
    {
        var previousState = new {IsActive, MatchingWindowHandle, ActiveWindowTitle, ActiveWindowHandle, ActiveProcessId};
        ActiveWindow = windowHandleProvider.GetByWindowHandle(hwnd);
        IsActive = windowMatcher.IsMatch(title, hwnd, processId);
        MatchingWindow = IsActive ? ActiveWindow : default;

        if (previousState.ActiveWindowHandle != ActiveWindowHandle)
        {
            Log.Debug(() => $"[#{Name}] Target window is {(IsActive ? string.Empty : "NOT ")}ACTIVE ({hwnd.ToHexadecimal()}, title '{ActiveWindowTitle}')");
        }

        this.RaiseIfChanged(nameof(IsActive), previousState.IsActive, IsActive);
        this.RaiseIfChanged(nameof(MatchingWindowHandle), previousState.MatchingWindowHandle, MatchingWindowHandle);
        this.RaiseIfChanged(nameof(ActiveWindowTitle), previousState.ActiveWindowTitle, ActiveWindowTitle);
        this.RaiseIfChanged(nameof(ActiveWindowHandle), previousState.ActiveWindowHandle, ActiveWindowHandle);
        this.RaiseIfChanged(nameof(ActiveProcessId), previousState.ActiveProcessId, ActiveProcessId);
    }
}