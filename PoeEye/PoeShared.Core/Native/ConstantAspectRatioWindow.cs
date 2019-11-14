using System;
using System.Diagnostics;
using System.Drawing;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using log4net;
using PoeShared.Scaffolding;
using Point = System.Drawing.Point;

namespace PoeShared.Native
{
    public class ConstantAspectRatioWindow : Window
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ConstantAspectRatioWindow));

        public static readonly DependencyProperty TargetAspectRatioProperty = DependencyProperty.Register(
            "TargetAspectRatio",
            typeof(double?),
            typeof(ConstantAspectRatioWindow),
            new PropertyMetadata(default(double?)));

        private readonly CompositeDisposable anchors = new CompositeDisposable();

        private readonly AspectRatioSizeCalculator aspectRatioSizeCalculator = new AspectRatioSizeCalculator();
        private DragParams? dragParams;

        protected ConstantAspectRatioWindow()
        {
            SourceInitialized += Window_SourceInitialized;
            this.Observe(TargetAspectRatioProperty)
                .Select(() => TargetAspectRatio)
                .DistinctUntilChanged()
                .Subscribe(
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
                    })
                .AddTo(anchors);
        }

        public double? TargetAspectRatio
        {
            get => (double?) GetValue(TargetAspectRatioProperty);
            set => SetValue(TargetAspectRatioProperty, value);
        }

        protected override void OnClosed(EventArgs e)
        {
            Log.Debug($"Window is closed, source: {this}");

            anchors.Dispose();
            base.OnClosed(e);
        }

        private void Window_SourceInitialized(object sender, EventArgs ea)
        {
            Log.Debug($"Initializing windowSource, source: {this}");
            var hwndSource = (HwndSource) PresentationSource.FromVisual(this);
            if (hwndSource == null)
            {
                throw new ApplicationException("HwndSource must be initialized at this point");
            }
            
            hwndSource.AddHook(DragHook);
        }

        private IntPtr DragHook(IntPtr hwnd, int msgRaw, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (TargetAspectRatio == null)
            {
                return IntPtr.Zero;
            }

            var msg = (WM) msgRaw;
            switch (msg)
            {
                case WM.ENTERSIZEMOVE:
                {
                    var thisWindow = new WindowInteropHelper(this).Handle;
                    var bounds = UnsafeNative.GetWindowRect(thisWindow);
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
                case WM.EXITSIZEMOVE:
                {
                    Log.Debug(
                        $"Drag mode completed for window {this}, initialBounds: {dragParams?.InitialBounds} => {new Rectangle((int) Left, (int) Top, (int) Width, (int) Height)}");
                    dragParams = null;
                    break;
                }
                case WM.WINDOWPOSCHANGING:
                {
                    if (dragParams == null)
                    {
                        break;
                    } 
                    
                    var pos = (WINDOWPOS) Marshal.PtrToStructure(lParam, typeof(WINDOWPOS));
                    if ((pos.flags & (int) SWP.NOMOVE) != 0)
                    {
                        break;
                    }
                    var bounds = new Rectangle(pos.x, pos.y, pos.cx, pos.cy);

                   

                    var newBounds = aspectRatioSizeCalculator.Calculate(TargetAspectRatio.Value, bounds, dragParams.Value.InitialBounds);
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
            }

            return IntPtr.Zero;
        }

        private enum SWP
        {
            NOMOVE = 0x0002
        }

        private enum WM
        {
            SIZE = 0x0005,
            NCCALCSIZE = 0x0083,
            WINDOWPOSCHANGING = 0x0046,
            EXITSIZEMOVE = 0x0232,
            ENTERSIZEMOVE = 0x0231
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WINDOWPOS
        {
            public readonly IntPtr hwnd;
            public readonly IntPtr hwndInsertAfter;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public readonly int flags;
        }

        private struct DragParams
        {
            public Rectangle InitialBounds { get; set; }
            public Point InitialMousePosition { get; set; }
            public bool AdjustingHeight { get; set; }
        }
    }
}