using System;
using System.Drawing;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using log4net;
using PInvoke;
using PoeShared.Scaffolding;
using PoeShared.Logging;
using PoeShared.UI;
using Point = System.Drawing.Point;

namespace PoeShared.Native
{
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

        private static long GlobalWindowId;

        protected readonly CompositeDisposable Anchors = new();

        private readonly AspectRatioSizeCalculator aspectRatioSizeCalculator = new();
        private DragParams? dragParams;

        protected ConstantAspectRatioWindow()
        {
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
                        if (!WindowsServices.SetWindowRect(thisWindow, newBounds))
                        {
                            Log.Warn($"Failed to assign initial window {this} ({thisWindow.ToHexadecimal()}) size, TargetAspectRatio: {targetAspectRatio}, initial bounds: {bounds}, target bounds: {newBounds}");
                        }
                    }, Log.HandleUiException)
                .AddTo(Anchors);
            Dpi = new PointF(1, 1);
        }

        private void OnInitialized(object? sender, EventArgs e)
        {
            Log.Debug(() => $"Window initialized");
            Log.Debug("Initializing native window handle");
            new WindowInteropHelper(this).EnsureHandle(); //EnsureHandle leads to SourceInitialized
            Log.Debug(() => "Native window initialized");
        }

        public IntPtr WindowHandle { get; private set; }

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
                throw new ApplicationException("Window handle must be initialized at this point");
            }
        }

        private void OnLoaded(object sender, EventArgs ea)
        {
            Log.Info($"Window is loaded");
            var hwndSource = (HwndSource)PresentationSource.FromVisual(this);
            if (hwndSource == null)
            {
                throw new ApplicationException("HwndSource must be initialized at this point");
            }

            Dpi = GetDpiFromHwndSource(hwndSource);
            hwndSource.AddHook(DragHook);
        }

        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        {
            return new NoopWindowAutomationPeer(this);
        }

        private IntPtr DragHook(IntPtr hwnd, int msgRaw, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (TargetAspectRatio == null)
            {
                return IntPtr.Zero;
            }

            if (handled)
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
}