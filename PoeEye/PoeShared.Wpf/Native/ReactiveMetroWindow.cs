//#define WINDOW_ENABLE_STACKTRACE_LOG

using System;
using System.Drawing;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using PInvoke;
using PoeShared.Scaffolding;
using PoeShared.Logging;
using PoeShared.UI;
using ReactiveUI;

namespace PoeShared.Native;

public class ReactiveMetroWindow : ReactiveMetroWindowBase
{
    public static readonly DependencyProperty TargetAspectRatioProperty = DependencyProperty.Register(
        nameof(TargetAspectRatio), typeof(double?), typeof(ReactiveMetroWindow), new PropertyMetadata(default(double?)));

    private const float DefaultPixelsPerInch = 96.0F;

    private readonly AspectRatioSizeCalculator aspectRatioSizeCalculator = new();
    private DragParams? dragParams;

    public ReactiveMetroWindow()
    {
        Log.Info(() => "Created window");
        Tag = $"Tag of {WindowId}";
        Loaded += OnLoaded;

        this.Observe(TargetAspectRatioProperty, x => x.TargetAspectRatio)
            .DistinctUntilChanged()
            .SubscribeSafe(
                targetAspectRatio =>
                {
                    if (targetAspectRatio == null)
                    {
                        return;
                    }

                    // Update window size
                    var thisWindow = new WindowInteropHelper(this).Handle;
                    var bounds = UnsafeNative.GetWindowRect(thisWindow);
                    var newBounds = aspectRatioSizeCalculator.Calculate(targetAspectRatio.Value, bounds, bounds,
                        prioritizeHeight: targetAspectRatio.Value >= 1);
                    if (newBounds == bounds)
                    {
                        return;
                    }
#if WINDOW_ENABLE_STACKTRACE_LOG
                    Log.Debug(() =>
                        $"Setting initial window bounds, TargetAspectRatio: {targetAspectRatio}, current bounds: {bounds}, desired bounds: {newBounds}");
#endif
                    NativeBounds = newBounds;
                }, Log.HandleUiException)
            .AddTo(Anchors);
        Dpi = new PointF(1, 1);
        Controller = new WindowViewController(this).AddTo(Anchors);
    }

    public IWindowViewController Controller { get; }

    public Rectangle NativeBounds { get; set; }

    public Rectangle ActualBounds { get; private set; }

    public double? TargetAspectRatio
    {
        get => (double?) GetValue(TargetAspectRatioProperty);
        set => SetValue(TargetAspectRatioProperty, value);
    }

    public PointF Dpi { get; private set; }

    public bool DpiAware { get; set; } = true;

    public IObservable<EventPattern<EventArgs>> WhenRendered => Observable
        .FromEventPattern<EventHandler, EventArgs>(h => ContentRendered += h, h => ContentRendered -= h);

    private void OnLoaded(object sender, EventArgs ea)
    {
        try
        {
            HandleLoaded();
        }
        catch (Exception e)
        {
            // there are some cases, e.g. when the window is closed very quickly,
            // when Loaded won't trigger fast enough and that will lead to exception
            Log.Warn("Failed to process Loaded", e);
        }
    }

    private IntPtr WindowPositionHook(IntPtr hwnd, int msgRaw, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        //this callback is called on UI thread and handles all messages sent to window
        if (handled || lParam == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        var msg = (User32.WindowMessage) msgRaw;
        switch (msg)
        {
            case User32.WindowMessage.WM_GETMINMAXINFO
                when Marshal.PtrToStructure(lParam, typeof(User32.MINMAXINFO)) is User32.MINMAXINFO minmax:
            {
#if WINDOW_ENABLE_STACKTRACE_LOG
                Log.WithSuffix(msg).Debug(() => $"OS has requested window MinMaxInfo, structure value: {minmax.ToJson()}");
#endif
                minmax.ptMinTrackSize = new POINT(); // there is a problem with WPF Window which measures MinSize incorrectly 
                Marshal.StructureToPtr(minmax, lParam, true);
#if WINDOW_ENABLE_STACKTRACE_LOG
                Log.WithSuffix(msg).Debug(() => $"Overriding MinMaxInfo with new value: {minmax.ToJson()}");
#endif
                break;
            }
            case User32.WindowMessage.WM_WINDOWPOSCHANGING
                when Marshal.PtrToStructure(lParam, typeof(UnsafeNative.WINDOWPOS)) is UnsafeNative.WINDOWPOS wp:
            {
                if (wp.flags.HasFlag(User32.SetWindowPosFlags.SWP_NOMOVE | User32.SetWindowPosFlags.SWP_NOSIZE))
                {
                    // window reordering
#if WINDOW_ENABLE_STACKTRACE_LOG
                    Log.WithSuffix(msg).Debug(() => $"Window position is being updated w/o move/resize, flags: {wp.flags}");
#endif
                    break;
                }

                var desiredBounds = new Rectangle(wp.x, wp.y, wp.cx, wp.cy);
#if WINDOW_ENABLE_STACKTRACE_LOG
                Log.WithSuffix(msg).Debug(() => $"Window position is being changed to {desiredBounds}, flags: {wp.flags}");
#endif
                break;
            }
            case User32.WindowMessage.WM_SIZING
                when Marshal.PtrToStructure(lParam, typeof(RECT)) is RECT bounds:
            {
#if WINDOW_ENABLE_STACKTRACE_LOG
                Log.WithSuffix(msg).Debug(() => $"Window size is being changed to {bounds}");
#endif
                break;
            }
            case User32.WindowMessage.WM_SIZE:
            {
                // The low-order word of lParam specifies the new width of the client area.
                // The high-order word of lParam specifies the new height of the client area.
                var newSize = new WinSize(lParam.LoWord(), lParam.HiWord());
#if WINDOW_ENABLE_STACKTRACE_LOG
                Log.WithSuffix(msg).Debug(() => $"Window size has been changed to {newSize}");
#endif
                break;
            }
            case User32.WindowMessage.WM_WINDOWPOSCHANGED
                when Marshal.PtrToStructure(lParam, typeof(UnsafeNative.WINDOWPOS)) is UnsafeNative.WINDOWPOS wp:
            {
                if (wp.flags.HasFlag(User32.SetWindowPosFlags.SWP_NOMOVE | User32.SetWindowPosFlags.SWP_NOSIZE))
                {
                    // window reordering
#if WINDOW_ENABLE_STACKTRACE_LOG
                    Log.WithSuffix(msg).Debug(() => $"Window position is being updated w/o move/resize, flags: {wp.flags}");
#endif
                    break;
                }

                /* When a window is minimized, Windows doesn't actually move the window off-screen in the sense of simply placing it somewhere far away from the visible display area.
                 * Instead, Windows gives it a specific off-screen position, which is at the coordinates (-32000, -32000).
                 * This is a special coordinate used internally by the windowing system to denote minimized windows.
                 * If you were to enumerate all windows on the system and check their positions, any window that is minimized would report this position. */
                if (wp is {x: -32000, y: -32000})
                {
#if WINDOW_ENABLE_STACKTRACE_LOG
                    Log.WithSuffix(msg).Debug(() => $"Window position is being updated w/o move/resize due to minimize/maximize operation");
#endif
                    break;
                }

                var newBounds = new Rectangle(wp.x, wp.y, wp.cx, wp.cy);
#if WINDOW_ENABLE_STACKTRACE_LOG
                Log.WithSuffix(msg).Debug(() => $"Window position has been changed to {newBounds}, flags: {wp.flags}");
#endif

                var currentBounds = ActualBounds;
                if (newBounds != currentBounds)
                {
#if WINDOW_ENABLE_STACKTRACE_LOG
                    Log.WithSuffix(msg).Debug(() => $"Updating actual bounds: {currentBounds} => {newBounds}");
#endif
                    ActualBounds = newBounds;
#if WINDOW_ENABLE_STACKTRACE_LOG
                    Log.WithSuffix(msg).Debug(() => $"Updated actual bounds: {currentBounds} => {newBounds}");
#endif
                }

                break;
            }
        }

        return IntPtr.Zero;
    }

    protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
    {
        return new NoopWindowAutomationPeer(this);
    }

    private void HandleLoaded()
    {
        Log.Info(() => $"Window is loaded");
#if WINDOW_ENABLE_STACKTRACE_LOG
        Log.Debug(() => $"Resolving {nameof(HwndSource)} for {WindowHandle}");
#endif
        var hwndSource = (HwndSource) PresentationSource.FromVisual(this);
        if (hwndSource == null)
        {
            throw new InvalidStateException("HwndSource must be initialized at this point");
        }

        Disposable.Create(() =>
        {
#if WINDOW_ENABLE_STACKTRACE_LOG
            Log.Debug(() => $"Releasing {nameof(HwndSource)}");
#endif
            hwndSource.Dispose();
        }).AddTo(Anchors);

        this.WhenAnyValue(ShowSystemMenuProperty, x => x.ShowSystemMenu)
            .Subscribe(x =>
            {
                if (x)
                {
#if WINDOW_ENABLE_STACKTRACE_LOG
                    Log.Debug(() => "Showing system menu");
#endif
                    UnsafeNative.ShowSystemMenu(WindowHandle);
                }
                else
                {
#if WINDOW_ENABLE_STACKTRACE_LOG
                    Log.Debug(() => "Hiding system menu");
#endif
                    UnsafeNative.HideSystemMenu(WindowHandle);
                }
            })
            .AddTo(Anchors);

        Dpi = GetDpiFromHwndSource(hwndSource);
        hwndSource.AddHook(WindowDragHook);
        //Callback will happen on a OverlayWindow UI thread, usually it's app main UI thread
#if WINDOW_ENABLE_STACKTRACE_LOG
        Log.Debug(() => $"Resolved {nameof(HwndSource)} for {WindowHandle}: {hwndSource}");
#endif
        hwndSource.AddHook(WindowPositionHook);

        // this sync mechanism is needed to keep NativeBounds in sync with real current window position WITHOUT getting into recursive assignments
        // i.e. Real position changes => NativeBounds tries to sync, fails to do so due to rounding or any other mechanism => changes window bounds => real position changes...
        var isUpdatingActualBounds = false;
        this.WhenAnyValue(x => x.NativeBounds)
            .WithPrevious()
            .Where(x => x.Current != x.Previous)
            .SubscribeSafe(x =>
            {
                // WARNING - Get/SetWindowRect are blocking as they await for WndProc to process the corresponding WM_* messages
                if (isUpdatingActualBounds)
                {
#if WINDOW_ENABLE_STACKTRACE_LOG
                    Log.Debug(() =>
                        $"Native bounds changed as a part of actual bounds update: {x.Previous} => {x.Current}");
#endif
                    return;
                }

#if WINDOW_ENABLE_STACKTRACE_LOG
                Log.Debug(() => $"Native bounds changed, setting windows rect: {x.Previous} => {x.Current}");
#endif
                UnsafeNative.SetWindowRect(WindowHandle, x.Current);
                var actualBounds = UnsafeNative.GetWindowRect(WindowHandle);
                if (actualBounds != x.Current)
                {
#if WINDOW_ENABLE_STACKTRACE_LOG
                    Log.Warn(() => $"Failed to resize: {x.Previous} => {x.Current}, resulting native bounds: {actualBounds}");
#endif
                }
                else
                {
#if WINDOW_ENABLE_STACKTRACE_LOG
                    Log.Debug(() => $"Native bounds changed: {x.Previous} => {x.Current}");
#endif
                }
            }, Log.HandleUiException)
            .AddTo(Anchors);

        this.WhenAnyValue(x => x.ActualBounds)
            .WithPrevious()
            .Where(x => x.Current != x.Previous)
            .SubscribeSafe(x =>
            {
                if (NativeBounds == x.Current)
                {
                    return;
                }

#if WINDOW_ENABLE_STACKTRACE_LOG
                Log.Debug(() => $"Actual bounds have changed: {x.Previous} => {x.Current}");
#endif
                try
                {
                    isUpdatingActualBounds = true;
                    NativeBounds = x.Current;
                }
                finally
                {
                    isUpdatingActualBounds = false;
                }

#if WINDOW_ENABLE_STACKTRACE_LOG
                Log.Debug(() => $"Propagated actual bounds: {x.Current}");
#endif
            }, Log.HandleUiException)
            .AddTo(Anchors);
    }

    private IntPtr WindowDragHook(IntPtr hwnd, int msgRaw, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (handled || TargetAspectRatio == null)
        {
            return IntPtr.Zero;
        }

        var msg = (User32.WindowMessage) msgRaw;
        switch (msg)
        {
            case User32.WindowMessage.WM_ENTERSIZEMOVE:
            {
                var bounds = ActualBounds;
                dragParams = new DragParams
                {
                    InitialBounds = bounds,
                    InitialAspectRatio = (double) bounds.Width / bounds.Height,
                };
#if WINDOW_ENABLE_STACKTRACE_LOG
                Log.Debug(() => $"Entering Drag mode, initial bounds: {bounds}");
#endif

                break;
            }
            case User32.WindowMessage.WM_EXITSIZEMOVE:
            {
                if (dragParams == null)
                {
                    break;
                }

#if WINDOW_ENABLE_STACKTRACE_LOG

                Log.Debug(() => $"Drag mode completed, initialBounds: {dragParams?.InitialBounds} => {ActualBounds}");
#endif

                dragParams = null;
                break;
            }
            case User32.WindowMessage.WM_WINDOWPOSCHANGING:
            {
                if (dragParams == null)
                {
                    break;
                }

                var pos = (UnsafeNative.WINDOWPOS) Marshal.PtrToStructure(lParam, typeof(UnsafeNative.WINDOWPOS));
                if (pos.flags.HasFlag(User32.SetWindowPosFlags.SWP_NOMOVE))
                {
                    break;
                }

                var initialBounds = dragParams.Value.InitialBounds;
                var aspectRatio = TargetAspectRatio.Value;
                var minSize = new WpfSize(MinWidth, MinHeight).Scale(Dpi).ToWinSize();
                var maxSize = new WpfSize(MaxWidth, MaxHeight).Scale(Dpi).ToWinSize();
                var bounds = new Rectangle(pos.x, pos.y, pos.cx, pos.cy);
#if WINDOW_ENABLE_STACKTRACE_LOG
                var logSuffix =
                    $"initial bounds: {initialBounds}, targetAspectRatio: {aspectRatio}, move bounds: {bounds}";
#endif

                if (bounds.Size == initialBounds.Size)
                {
#if WINDOW_ENABLE_STACKTRACE_LOG
                    Log.WithSuffix(logSuffix).Debug(() => $"Ignoring position change - actual size stayed the same");
#endif
                    break;
                }

                var newBounds = aspectRatioSizeCalculator.Calculate(aspectRatio, bounds, initialBounds,
                    prioritizeHeight: aspectRatio >= 1);
#if WINDOW_ENABLE_STACKTRACE_LOG
                Log.WithSuffix(logSuffix).Debug(() => $"Calculated updated bounds: {newBounds}");
#endif
                newBounds.Width = newBounds.Width.EnsureInRange(minSize.Width, maxSize.Width);
                newBounds.Height = newBounds.Height.EnsureInRange(minSize.Height, maxSize.Height);
                if (newBounds == bounds)
                {
                    break;
                }

#if WINDOW_ENABLE_STACKTRACE_LOG
                Log.WithSuffix(logSuffix).Debug(() => $"Propagating updated bounds: {newBounds}");
#endif

                pos.x = newBounds.X;
                pos.y = newBounds.Y;
                pos.cx = newBounds.Width;
                pos.cy = newBounds.Height;

                Marshal.StructureToPtr(pos, lParam, true);
                handled = true;
                break;
            }
            case User32.WindowMessage.WM_DPICHANGED:
                if (PresentationSource.FromVisual(this) is HwndSource hwndSource)
                {
                    Dpi = GetDpiFromHwndSource(hwndSource);
                    if (DpiAware)
                    {
                        handled = true;
                    }
                }

                break;
        }

        return IntPtr.Zero;
    }

    private static PointF GetDpiFromHwndSource(HwndSource targetSource)
    {
        if (targetSource == null)
        {
            throw new ArgumentNullException(nameof(targetSource));
        }

        if (!UnsafeNative.IsWindows8OrGreater())
        {
            return default;
        }

        var handleMonitor = User32.MonitorFromWindow(
            targetSource.Handle,
            User32.MonitorOptions.MONITOR_DEFAULTTONEAREST);

        if (handleMonitor == IntPtr.Zero)
        {
            return default;
        }

        if (!UnsafeNative.IsWindows10OrGreater())
        {
            // SHCore is supported only on Win8.1+, it's safer to fallback to Win10
            return default;
        }

        var dpiResult =
            SHCore.GetDpiForMonitor(handleMonitor, MONITOR_DPI_TYPE.MDT_DEFAULT, out var dpiX, out var dpiY);
        if (dpiResult.Failed)
        {
            return default;
        }

        return new PointF(dpiX / DefaultPixelsPerInch, dpiY / DefaultPixelsPerInch);
    }

    private struct DragParams
    {
        public WinRect InitialBounds { get; set; }
        public double InitialAspectRatio { get; set; }
        public WinPoint InitialMousePosition { get; set; }
    }
}