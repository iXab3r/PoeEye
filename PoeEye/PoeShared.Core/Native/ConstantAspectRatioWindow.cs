using System;
using System.Drawing;
using System.Reactive.Disposables;
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

        private readonly AspectRatioSizeCalculator aspectRatioSizeCalculator = new AspectRatioSizeCalculator();
        private readonly CompositeDisposable anchors = new CompositeDisposable();
        private DragParams? dragParams;

        protected ConstantAspectRatioWindow()
        {
            SourceInitialized += Window_SourceInitialized;
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

        private IntPtr DragHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (TargetAspectRatio == null)
            {
                return IntPtr.Zero;
            }

            switch ((WM) msg)
            {
                case WM.EXITSIZEMOVE:
                    dragParams = null;
                    break;
                case WM.WINDOWPOSCHANGING:
                {
                    var pos = (WINDOWPOS) Marshal.PtrToStructure(lParam, typeof(WINDOWPOS));

                    if ((pos.flags & (int) SWP.NOMOVE) != 0)
                    {
                        return IntPtr.Zero;
                    }

                    var bounds = new Rectangle(pos.x, pos.y, pos.cx, pos.cy);

                    if (dragParams == null)
                    {
                        var p = UnsafeNative.GetMousePosition();

                        var diffWidth = Math.Min(Math.Abs(p.X - pos.x), Math.Abs(p.X - pos.x - pos.cx));
                        var diffHeight = Math.Min(Math.Abs(p.Y - pos.y), Math.Abs(p.Y - pos.y - pos.cy));

                        dragParams = new DragParams
                        {
                            AdjustingHeight = diffHeight > diffWidth,
                            InitialBounds = bounds
                        };
                    }

                    var newBounds = aspectRatioSizeCalculator.Calculate(TargetAspectRatio.Value, bounds, dragParams.Value.InitialBounds);

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