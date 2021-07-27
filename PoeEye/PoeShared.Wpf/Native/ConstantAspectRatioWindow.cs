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
using Point = System.Drawing.Point;

namespace PoeShared.Native
{
    public class ConstantAspectRatioWindow : Window
    {
        public static readonly DependencyProperty TargetAspectRatioProperty = DependencyProperty.Register(
            "TargetAspectRatio",
            typeof(double?),
            typeof(ConstantAspectRatioWindow),
            new PropertyMetadata(default(double?)));

        public static readonly DependencyProperty DpiProperty = DependencyProperty.Register(
            "Dpi", typeof(PointF), typeof(ConstantAspectRatioWindow), new PropertyMetadata(default(PointF)));

        public static readonly DependencyProperty DpiAwareProperty = DependencyProperty.Register(
            "DpiAware", typeof(bool), typeof(ConstantAspectRatioWindow), new PropertyMetadata(default(bool)));
        private static int GlobalWindowIdx = 0;
        private static readonly IFluentLog Log = typeof(ConstantAspectRatioWindow).PrepareLogger();
        private const float DefaultPixelsPerInch = 96.0F;
       
        private readonly CompositeDisposable anchors = new CompositeDisposable();

        private readonly AspectRatioSizeCalculator aspectRatioSizeCalculator = new AspectRatioSizeCalculator();
        private readonly Lazy<IntPtr> windowHandle;
        private DragParams? dragParams;

        protected ConstantAspectRatioWindow()
        {
            WindowIdx = Interlocked.Increment(ref GlobalWindowIdx);
            Title = $"Window {WindowIdx}";
            Tag = $"Tag of Window {WindowIdx}";
            
            windowHandle = new Lazy<IntPtr>(() => new WindowInteropHelper(this).EnsureHandle());
            Loaded += HandleWindowLoaded;
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
                        var newBounds = aspectRatioSizeCalculator.Calculate(targetAspectRatio.Value, bounds, bounds);
                        if (newBounds == bounds)
                        {
                            return;
                        }

                        Log.Debug($"Setting initial window {this} size ({thisWindow.ToHexadecimal()}), TargetAspectRatio: {targetAspectRatio}, current bounds: {bounds}, target bounds: {newBounds}");
                        if (!WindowsServices.SetWindowRect(thisWindow, newBounds))
                        {
                            Log.Warn($"Failed to assign initial window {this} ({thisWindow.ToHexadecimal()}) size, TargetAspectRatio: {targetAspectRatio}, initial bounds: {bounds}, target bounds: {newBounds}");
                        } 
                    }, Log.HandleUiException)
                .AddTo(anchors);
            Dpi = new PointF(1, 1);
        }
        
        public int WindowIdx { get; }

        public double? TargetAspectRatio
        {
            get => (double?) GetValue(TargetAspectRatioProperty);
            set => SetValue(TargetAspectRatioProperty, value);
        }
        
        public PointF Dpi
        {
            get { return (PointF) GetValue(DpiProperty); }
            set { SetValue(DpiProperty, value); }
        }
        
        public bool DpiAware
        {
            get { return (bool) GetValue(DpiAwareProperty); }
            set { SetValue(DpiAwareProperty, value); }
        }

        protected override void OnClosed(EventArgs e)
        {
            Log.Info($"Window is closed, source: {this}");

            anchors.Dispose();
            base.OnClosed(e);
        }

        private void HandleWindowLoaded(object sender, EventArgs ea)
        {
            Log.Info($"Initializing windowSource, source: {this}");
            var hwndSource = (HwndSource) PresentationSource.FromVisual(this);
            if (hwndSource == null)
            {
                throw new ApplicationException("HwndSource must be initialized at this point");
            }

            Dpi = GetDpiFromHwndSource(hwndSource);
            
            hwndSource.AddHook(DragHook);
        }

        private IntPtr DragHook(IntPtr hwnd, int msgRaw, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (TargetAspectRatio == null)
            {
                return IntPtr.Zero;
            }

            var msg = (User32.WindowMessage) msgRaw;
            switch (msg)
            {
                case User32.WindowMessage.WM_ENTERSIZEMOVE:
                {
                    var bounds = UnsafeNative.GetWindowRect(windowHandle.Value);
                    var p = UnsafeNative.GetMousePosition();
                    var diffWidth = Math.Min(Math.Abs(p.X - bounds.X), Math.Abs(p.X - bounds.X - bounds.Width));
                    var diffHeight = Math.Min(Math.Abs(p.Y - bounds.Y), Math.Abs(p.Y - bounds.Y - bounds.Height));

                    dragParams = new DragParams
                    {
                        AdjustingHeight = diffHeight > diffWidth,
                        InitialBounds = bounds
                    };

                    Log.Debug($"Entering Drag mode for window {this}, initialBounds: {bounds}, adjustingHeight: {dragParams.Value.AdjustingHeight}");
                    break;
                }
                case User32.WindowMessage.WM_EXITSIZEMOVE:
                {
                    Log.Debug(
                        $"Drag mode completed for window {this}, initialBounds: {dragParams?.InitialBounds} => {new Rectangle((int) Left, (int) Top, (int) Width, (int) Height)}");
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
                    if ((pos.flags & (int) SWP.NOMOVE) != 0)
                    {
                        break;
                    }
                    var bounds = new Rectangle(pos.x, pos.y, pos.cx, pos.cy);
                    var newBounds = aspectRatioSizeCalculator.Calculate(TargetAspectRatio.Value, bounds, dragParams.Value.InitialBounds);
                    newBounds.Width = (int)Math.Max(MinWidth, Math.Min(MaxWidth, newBounds.Width));
                    newBounds.Height = (int)Math.Max(MinHeight, Math.Min(MaxHeight, newBounds.Height));
                    Log.Debug(
                        $"Window pos changing, window: {this}, initialBounds: {dragParams?.InitialBounds} => {new Rectangle((int) Left, (int) Top, (int) Width, (int) Height)}, resize bounds: {bounds}, desired bounds: {newBounds}");

                    if (newBounds == bounds)
                    {
                        break;
                    }
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
            public Point InitialMousePosition { get; set; }
            public bool AdjustingHeight { get; set; }
        }
    }
}