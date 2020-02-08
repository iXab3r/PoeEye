using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using PInvoke;
using PoeShared.Scaffolding;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace PoeShared.Native
{
    public partial class UnsafeNative
    {
        public static Rect GetMonitorBounds(Window window)
        {
            var handle = window != null
                ? new WindowInteropHelper(window).Handle
                : IntPtr.Zero;
            return GetMonitorBounds(handle);
        }

        public static Rect GetMonitorBounds(IntPtr windowHandle)
        {
            var screen = Screen.FromHandle(windowHandle);
            using (var screenDc = User32.GetDC(windowHandle))
            {
                if (screenDc.IsInvalid)
                {
                    var error = Kernel32.GetLastError();
                    Log.Warn($"Failed to GetDC for screen {windowHandle.ToInt64()}, error: {error}");
                    return Rect.Empty;
                }
            
                var graphics = Graphics.FromHdc(screenDc.DangerousGetHandle());
                var dpi = GetDpi(graphics);
                Log.Debug($"Monitor for window {windowHandle.ToHexadecimal()}: {GetMonitorInfo(windowHandle)}");
                return CalculateScreenBounds(screen, dpi);
            }
        }

        private static Rect CalculateScreenBounds(Screen monitor, PointF dpi)
        {
            var result = new Rect(
                monitor.Bounds.X,
                monitor.Bounds.Y,
                monitor.Bounds.Width,
                monitor.Bounds.Height);
            result.Scale(1 / dpi.X, 1 / dpi.Y);
            return result;
        }

        public static bool IsOutOfBounds(Point point, Size bounds)
        {
            return IsOutOfBounds(point, new Rect(new Point(), bounds));
        }

        public static bool IsOutOfBounds(Rect frame, Rect bounds)
        {
            var downscaledFrame = frame;
            // downscaling frame as we do not require for FULL frame to be visible, only top-left part of it
            downscaledFrame.Size = frame.Size.Scale(0.25);

            return double.IsNaN(frame.X) ||
                   double.IsNaN(frame.Y) ||
                   double.IsNaN(frame.Width) ||
                   double.IsNaN(frame.Height) ||
                   downscaledFrame.X <= 1 ||
                   downscaledFrame.Y <= 1 ||
                   !bounds.Contains(downscaledFrame);
        }

        public static bool IsOutOfBounds(Point point, Rect bounds)
        {
            return double.IsNaN(point.X) ||
                   double.IsNaN(point.Y) ||
                   point.X <= 1 ||
                   point.Y <= 1 ||
                   !bounds.IntersectsWith(new Rect(point.X, point.Y, 1, 1));
        }

        public static Point GetPositionAtTheCenter(Rect monitorBounds, Size windowSize)
        {
            var screenCenter = new Point(
                monitorBounds.X + monitorBounds.Width / 2,
                monitorBounds.Y + monitorBounds.Height / 2);
            screenCenter.Offset(-windowSize.Width / 2, -windowSize.Height / 2);

            return screenCenter;
        }

        public static Point GetPositionAtTheCenter(Window window)
        {
            var monitorBounds = GetMonitorBounds(window);
            return GetPositionAtTheCenter(monitorBounds, new Size(window.Width, window.Height));
        }
    }
}