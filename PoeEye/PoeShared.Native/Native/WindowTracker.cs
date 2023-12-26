using System;
using System.Reactive.Linq;
using System.Threading;
using PInvoke;
using PoeShared.Logging;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.Native;

public class WindowTracker : DisposableReactiveObjectWithLogger, IWindowTracker
{
    private static long GlobalIdx = 0;
    private readonly IWindowTrackerMatcher windowMatcher;
    private readonly string instanceId = $"Tracker#{Interlocked.Increment(ref GlobalIdx)}";

    public WindowTracker(
        IForegroundWindowTracker foregroundWindowTracker,
        IWindowTrackerMatcher windowMatcher)
    {
        Guard.ArgumentNotNull(windowMatcher, nameof(windowMatcher));
        Log.AddSuffix(instanceId);

        this.windowMatcher = windowMatcher;

        foregroundWindowTracker.WhenAnyValue(x => x.ForegroundWindow)
            .Subscribe(WindowActivated)
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
            Log.Debug($"[#{Name}] Target window is {(IsActive ? string.Empty : "NOT ")}ACTIVE ({window.Handle.ToHexadecimal()}, title '{ActiveWindowTitle}')");
        }

        this.RaiseIfChanged(nameof(IsActive), previousState.IsActive, IsActive);
        this.RaiseIfChanged(nameof(MatchingWindowHandle), previousState.MatchingWindowHandle, MatchingWindowHandle);
        this.RaiseIfChanged(nameof(ActiveWindowTitle), previousState.ActiveWindowTitle, ActiveWindowTitle);
        this.RaiseIfChanged(nameof(ActiveWindowHandle), previousState.ActiveWindowHandle, ActiveWindowHandle);
        this.RaiseIfChanged(nameof(ActiveProcessId), previousState.ActiveProcessId, ActiveProcessId);
    }
}