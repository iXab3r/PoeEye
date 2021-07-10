using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using PInvoke;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
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
        
        public static Rectangle GetMonitorBounds(Rectangle rect)
        {
            return System.Windows.Forms.Screen.GetBounds(rect);
        }

        public Screen GetScreen(Window window)
        {
            var handle = window != null
                ? new WindowInteropHelper(window).Handle
                : IntPtr.Zero;
            return Screen.FromHandle(handle);
        }

        public IntPtr GetMonitorFromRect(Rectangle rect)
        {
            var result = new RECT { top = rect.Top, left = rect.Left, bottom = rect.Bottom, right = rect.Right };
            return User32.MonitorFromRect(ref result, User32.MonitorOptions.MONITOR_DEFAULTTONEAREST);
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
                   downscaledFrame.X <= bounds.X ||
                   downscaledFrame.Y <= bounds.Y ||
                   !bounds.Contains(downscaledFrame);
        }

        public static bool IsOutOfBounds(Rectangle frame, Rectangle bounds)
        {
            var downscaledFrame = frame;
            // downscaling frame as we do not require for FULL frame to be visible, only top-left part of it
            downscaledFrame.Size = new System.Drawing.Size((int)(frame.Width * 0.25), (int)(frame.Height * 0.25));
            return downscaledFrame.X < bounds.X ||
                   downscaledFrame.Y < bounds.Y ||
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

        public static void ShowWindow(Window mainWindow)
        {
            Guard.ArgumentNotNull(() => mainWindow);
            Log.Debug($"ShowWindow command executed, windowState: {mainWindow.WindowState}");

            Log.Debug($"Activating main window, title: '{mainWindow.Title}' {new Point(mainWindow.Left, mainWindow.Top)}, isActive: {mainWindow.IsActive}, state: {mainWindow.WindowState}, topmost: {mainWindow.Topmost}, style:{mainWindow.WindowStyle}");

            if (mainWindow.Topmost)
            {
                mainWindow.Topmost = false;
                mainWindow.Topmost = true;
            }
            
            var mainWindowHelper = new WindowInteropHelper(mainWindow);
            var mainWindowHandle = mainWindowHelper.EnsureHandle();

            Log.Debug($"Showing main window, hWnd: {mainWindowHandle.ToHexadecimal()}, windowState: {mainWindow.WindowState}");
            mainWindow.Show();

            if (mainWindow.WindowState == WindowState.Minimized)
            {
                mainWindow.WindowState = WindowState.Normal;
            }

            if (mainWindowHandle != IntPtr.Zero && UnsafeNative.GetForegroundWindow() != mainWindowHandle)
            {
                Log.Debug($"Setting foreground window, hWnd: {mainWindowHandle.ToHexadecimal()}, windowState: {mainWindow.WindowState}");
                if (!UnsafeNative.SetForegroundWindow(mainWindowHandle))
                {
                    Log.Debug($"Failed to set foreground window, hWnd: {mainWindowHandle.ToHexadecimal()}");
                }
            }
        }

        public static void HideWindow(Window mainWindow)
        {
            Guard.ArgumentNotNull(() => mainWindow);

            Log.Debug($"HideWindow command executed, windowState: {mainWindow.WindowState}");
            mainWindow.Hide();
        }
    }
}