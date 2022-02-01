using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using PInvoke;
using PoeShared.Scaffolding;
using PoeShared.Logging;
using PoeShared.UI;
using Point = System.Drawing.Point;

namespace PoeShared.Native;

public class ConstantAspectRatioWindow : Window
{
    private const float DefaultPixelsPerInch = 96.0F;

    public static readonly DependencyProperty TargetAspectRatioProperty = DependencyProperty.Register(
        "TargetAspectRatio",
        typeof(double?),
        typeof(ConstantAspectRatioWindow),
        new PropertyMetadata(default(double?)));

    public static readonly DependencyProperty DpiProperty = DependencyProperty.Register(
        "Dpi", typeof(PointF), typeof(ConstantAspectRatioWindow), new PropertyMetadata(default(PointF)));

    public static readonly DependencyProperty DpiAwareProperty = DependencyProperty.Register(
        "DpiAware", typeof(bool), typeof(ConstantAspectRatioWindow), new PropertyMetadata(default(bool)));

    public static readonly DependencyProperty NativeBoundsProperty = DependencyProperty.Register(
        "NativeBounds", typeof(Rectangle), typeof(ConstantAspectRatioWindow), new PropertyMetadata(default(Rectangle)));

    private static long GlobalWindowId;

    public static readonly DependencyProperty ActualBoundsProperty = DependencyProperty.Register(
        "ActualBounds", typeof(Rectangle), typeof(ConstantAspectRatioWindow), new PropertyMetadata(default(Rectangle)));

    protected readonly CompositeDisposable Anchors = new();

    private readonly AspectRatioSizeCalculator aspectRatioSizeCalculator = new();
    private DragParams? dragParams;

    protected ConstantAspectRatioWindow()
    {
        Scheduler = new DispatcherScheduler(Dispatcher.CurrentDispatcher);
        Title = WindowId;
        Tag = $"Tag of {WindowId}";
        Log = typeof(ConstantAspectRatioWindow).PrepareLogger()
            .WithSuffix(WindowId)
            .WithSuffix(() => NativeWindowId)
            .WithSuffix(() => DataContext == default ? "Data context is not set" : DataContext.ToString());
        Loaded += OnLoaded;
        Initialized += OnInitialized;
        SourceInitialized += OnSourceInitialized;
        Closed += OnClosed;

        this.Observe(TargetAspectRatioProperty)
            .Select(_ => TargetAspectRatio)
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
                    var newBounds = aspectRatioSizeCalculator.Calculate(targetAspectRatio.Value, bounds, bounds, prioritizeHeight: targetAspectRatio.Value >= 1);
                    if (newBounds == bounds)
                    {
                        return;
                    }

                    Log.Debug(() => $"Setting initial window {this} size ({thisWindow.ToHexadecimal()}), TargetAspectRatio: {targetAspectRatio}, current bounds: {bounds}, target bounds: {newBounds}");
                    if (!UnsafeNative.SetWindowRect(thisWindow, newBounds))
                    {
                        Log.Warn($"Failed to assign initial window {this} ({thisWindow.ToHexadecimal()}) size, TargetAspectRatio: {targetAspectRatio}, initial bounds: {bounds}, target bounds: {newBounds}");
                    }
                }, Log.HandleUiException)
            .AddTo(Anchors);
        Dpi = new PointF(1, 1);
    }

    public Rectangle NativeBounds
    {
        get { return (Rectangle)GetValue(NativeBoundsProperty); }
        set { SetValue(NativeBoundsProperty, value); }
    }

    public Rectangle ActualBounds
    {
        get { return (Rectangle)GetValue(ActualBoundsProperty); }
        set { SetValue(ActualBoundsProperty, value); }
    }

    public IntPtr WindowHandle { get; private set; }

    public IScheduler Scheduler { get; }

    public string NativeWindowId => WindowHandle == IntPtr.Zero ? $"Native window not created yet" : WindowHandle.ToHexadecimal();

    public string WindowId { get; } = $"Wnd#{Interlocked.Increment(ref GlobalWindowId)}";

    public double? TargetAspectRatio
    {
        get => (double?)GetValue(TargetAspectRatioProperty);
        set => SetValue(TargetAspectRatioProperty, value);
    }

    public PointF Dpi
    {
        get { return (PointF)GetValue(DpiProperty); }
        set { SetValue(DpiProperty, value); }
    }

    public bool DpiAware
    {
        get { return (bool)GetValue(DpiAwareProperty); }
        set { SetValue(DpiAwareProperty, value); }
    }

    protected IFluentLog Log { get; }

    private void OnInitialized(object? sender, EventArgs e)
    {
        Log.Debug(() => $"Window initialized");
        Log.Debug("Initializing native window handle");
        new WindowInteropHelper(this).EnsureHandle(); //EnsureHandle leads to SourceInitialized
        Log.Debug(() => "Native window initialized");
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        Log.Info($"Window is closed, source: {this}");

        Anchors.Dispose();
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        Log.Debug(() => "Native window initialized");
        WindowHandle = new WindowInteropHelper(this).Handle; // should be already available here
        Log.Debug(() => $"Initialized native window handle");
        if (WindowHandle == IntPtr.Zero)
        {
            throw new InvalidStateException("Window handle must be initialized at this point");
        }
    }

    private void OnLoaded(object sender, EventArgs ea)
    {
        Log.Info($"Window is loaded");
        Log.Debug(() => $"Resolving {nameof(HwndSource)} for {WindowHandle}");
        var hwndSource = (HwndSource)PresentationSource.FromVisual(this);
        if (hwndSource == null)
        {
            throw new InvalidStateException("HwndSource must be initialized at this point");
        }

        Disposable.Create(() =>
        {
            Log.Debug(() => $"Releasing {nameof(HwndSource)}");
            hwndSource.Dispose();
        }).AddTo(Anchors);

        Dpi = GetDpiFromHwndSource(hwndSource);
        hwndSource.AddHook(WindowDragHook);
        //Callback will happen on a OverlayWindow UI thread, usually it's app main UI thread
        Log.Debug(() => $"Resolved {nameof(HwndSource)} for {WindowHandle}: {hwndSource}");
        hwndSource.AddHook(WindowPositionHook);

        // this sync mechanism is needed to keep NativeBounds in sync with real current window position WITHOUT getting into recursive assignments
        // i.e. Real position changes => NativeBounds tries to sync, fails to do so due to rounding or any other mechanism => changes window bounds => real position changes...
        this.Observe(ActualBoundsProperty, x => x.ActualBounds)
            .WithPrevious()
            .Where(x => x.Current != x.Previous)
            .SubscribeSafe(x =>
            {
                if (NativeBounds == x.Current)
                {
                    return;
                }
                Log.Info(() => $"Actual bounds have changed: {x.Previous} => {x.Current}");
                SetCurrentValue(NativeBoundsProperty, x.Current);
                Log.Info(() => $"Propagated actual bounds: {x.Current}");
            }, Log.HandleUiException)
            .AddTo(Anchors);
        
        this.Observe(NativeBoundsProperty, x => x.NativeBounds)
            .WithPrevious()
            .Where(x => x.Current != x.Previous)
            .SubscribeSafe(x =>
            {
                // WARNING - Get/SetWindowRect are blocking as they await for WndProc to process the corresponding WM_* messages
                Log.Info(() => $"Native bounds changed, setting windows rect: {x.Previous} => {x.Current}");
                UnsafeNative.SetWindowRect(WindowHandle, x.Current);
                var actualBounds = UnsafeNative.GetWindowRect(WindowHandle);
                if (actualBounds != x.Current)
                {
                    Log.Warn(() => $"Failed to resize: {x.Previous} => {x.Current}, resulting native bounds: {actualBounds}");
                }
                else
                {
                    Log.Info(() => $"Native bounds changed: {x.Previous} => {x.Current}");
                }
            }, Log.HandleUiException)
            .AddTo(Anchors);
    }

    private IntPtr WindowPositionHook(IntPtr hwnd, int msgRaw, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        //this callback is called on UI thread and handles all messages sent to window
        if (handled || lParam == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        var msg = (User32.WindowMessage)msgRaw;
        switch (msg)
        {
            case User32.WindowMessage.WM_WINDOWPOSCHANGED when Marshal.PtrToStructure(lParam, typeof(UnsafeNative.WINDOWPOS)) is UnsafeNative.WINDOWPOS wp:
            {
                var newBounds = new Rectangle(wp.x, wp.y, wp.cx, wp.cy);
                var currentBounds = ActualBounds;
                if (newBounds != currentBounds)
                {
                    Log.WithSuffix(msg).Info(() => $"Updating actual bounds: {currentBounds} => {newBounds}");
                    SetCurrentValue(ActualBoundsProperty, newBounds);
                    Log.WithSuffix(msg).Info(() => $"Updated actual bounds: {currentBounds} => {newBounds}");
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

    private IntPtr WindowDragHook(IntPtr hwnd, int msgRaw, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (handled || TargetAspectRatio == null )
        {
            return IntPtr.Zero;
        }

        var msg = (User32.WindowMessage)msgRaw;
        switch (msg)
        {
            case User32.WindowMessage.WM_ENTERSIZEMOVE:
            {
                var bounds = UnsafeNative.GetWindowRect(WindowHandle);
                var p = UnsafeNative.GetMousePosition();
                var diffWidth = Math.Min(Math.Abs(p.X - bounds.X), Math.Abs(p.X - bounds.X - bounds.Width));
                var diffHeight = Math.Min(Math.Abs(p.Y - bounds.Y), Math.Abs(p.Y - bounds.Y - bounds.Height));

                dragParams = new DragParams
                {
                    AdjustingHeight = diffHeight > diffWidth,
                    InitialBounds = bounds,
                    InitialAspectRatio = (double)bounds.Width / bounds.Height,
                };

                Log.Debug(() => $"Entering Drag mode for window {this}, initialBounds: {bounds}, adjustingHeight: {dragParams.Value.AdjustingHeight}");
                break;
            }
            case User32.WindowMessage.WM_EXITSIZEMOVE:
            {
                Log.Debug(() => $"Drag mode completed for window {this}, initialBounds: {dragParams?.InitialBounds} => {new Rectangle((int)Left, (int)Top, (int)Width, (int)Height)}");
                dragParams = null;
                break;
            }
            case User32.WindowMessage.WM_WINDOWPOSCHANGING:
            {
                if (dragParams == null)
                {
                    break;
                }

                var pos = (UnsafeNative.WINDOWPOS)Marshal.PtrToStructure(lParam, typeof(UnsafeNative.WINDOWPOS));
                if ((pos.flags & (int)SWP.NOMOVE) != 0)
                {
                    break;
                }

                var bounds = new Rectangle(pos.x, pos.y, pos.cx, pos.cy);
                var newBounds = aspectRatioSizeCalculator.Calculate(TargetAspectRatio.Value, bounds, dragParams.Value.InitialBounds, prioritizeHeight: TargetAspectRatio.Value >= 1);
                newBounds.Width = (int)Math.Max(MinWidth, Math.Min(MaxWidth, newBounds.Width));
                newBounds.Height = (int)Math.Max(MinHeight, Math.Min(MaxHeight, newBounds.Height));
                if (newBounds == bounds)
                {
                    break;
                }

                Log.Debug(() => $"In scope of resize to {bounds} of window: {this}( initial drag bounds: {dragParams?.InitialBounds}), targetAspectRatio: {TargetAspectRatio.Value} resizing to these bounds instead: {newBounds}");

                pos.x = newBounds.X;
                pos.y = newBounds.Y;
                pos.cx = newBounds.Width;
                pos.cy = newBounds.Height;

                Marshal.StructureToPtr(pos, lParam, true);
                handled = true;
                break;
            }
            case User32.WindowMessage.WM_DPICHANGED:
                Dpi = GetDpiFromHwndSource(PresentationSource.FromVisual(this) as HwndSource);
                if (DpiAware)
                {
                    handled = true;
                }

                break;
        }

        return IntPtr.Zero;
    }

    public override string ToString()
    {
        return $"{WindowId} ({NativeWindowId})";
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

        var dpiResult = SHCore.GetDpiForMonitor(handleMonitor, MONITOR_DPI_TYPE.MDT_DEFAULT, out var dpiX, out var dpiY);
        if (dpiResult.Failed)
        {
            return default;
        }

        return new PointF(dpiX / DefaultPixelsPerInch, dpiY / DefaultPixelsPerInch);
    }

    private enum SWP
    {
        NOMOVE = 0x0002
    }

    private struct DragParams
    {
        public Rectangle InitialBounds { get; set; }
        public double InitialAspectRatio { get; set; }
        public Point InitialMousePosition { get; set; }
        public bool AdjustingHeight { get; set; }
    }
}