using System;
using System.Drawing;
using System.Linq;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using PInvoke;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace PoeShared.Native
{
    public partial class UnsafeNative
    {
        public static IntPtr GetTopmostHwnd(IntPtr[] handles)
        {
            var topmostHwnd = IntPtr.Zero;

            if (handles == null || !handles.Any())
            {
                return topmostHwnd;
            }

            var hwnd = handles[0];

            while (hwnd != IntPtr.Zero)
            {
                if (handles.Contains(hwnd))
                {
                    topmostHwnd = hwnd;
                }

                hwnd = User32.GetWindow(hwnd, User32.GetWindowCommands.GW_HWNDPREV);
            }

            return topmostHwnd;
        }

        public static string GetMonitorInfo(Window window)
        {
            var handle = window != null
                ? new WindowInteropHelper(window).Handle
                : IntPtr.Zero;
            return GetMonitorInfo(handle);
        }

        public static string GetDesktopMonitorInfo()
        {
            var desktopWindow = User32.GetDesktopWindow();
            return GetMonitorInfo(desktopWindow);
        }

        public static string GetMonitorInfo(IntPtr windowHandle)
        {
            var screen = Screen.FromHandle(windowHandle);
            using (var screenDc = User32.GetDC(windowHandle))
            {
                if (screenDc.IsInvalid)
                {
                    var error = Kernel32.GetLastError();
                    Log.Warn($"Failed to GetDC for screen {windowHandle.ToInt64()}, error: {error}");
                    return $"ERROR: {error}";
                }

                var graphics = Graphics.FromHdc(screenDc.DangerousGetHandle());
                var scaledBounds = CalculateScreenBounds(screen, new PointF(graphics.DpiX, graphics.DpiY));
                return new
                {
                    screen.DeviceName, screen.Primary, graphics.PageScale, SystemBounds = screen.Bounds, ScaledBounds = scaledBounds, graphics.DpiX, graphics.DpiY
                }.ToString();
            }
        }

        public static PointF GetDesktopDpi()
        {
            var desktopWindow = User32.GetDesktopWindow();
            return GetDesktopDpi(desktopWindow);
        }

        public static PointF GetDesktopDpiFromPoint(Point location)
        {
            var desktopWindow = User32.MonitorFromPoint(new POINT {x = location.X, y = location.Y}, User32.MonitorOptions.MONITOR_DEFAULTTONULL);
            return desktopWindow == IntPtr.Zero ? PointF.Empty : GetDesktopDpi(desktopWindow);
        }

        public static PointF GetDesktopDpiFromWindow(Window window)
        {
            if (window == null)
            {
                return GetDesktopDpi();
            }

            var desktopWindow = new WindowInteropHelper(window).EnsureHandle();
            var desktop = User32.MonitorFromWindow(desktopWindow, User32.MonitorOptions.MONITOR_DEFAULTTONULL);
            return desktopWindow == IntPtr.Zero ? PointF.Empty : GetDesktopDpi(desktop);
        }

        public static PointF GetSystemDpi()
        {
            using (var screenHandle = User32.GetDC(IntPtr.Zero))
            {
                return new PointF(
                    Gdi32.GetDeviceCaps(screenHandle, Gdi32.DeviceCap.LOGPIXELSX),
                    Gdi32.GetDeviceCaps(screenHandle, Gdi32.DeviceCap.LOGPIXELSY));
            }
        }

        public static PointF GetDesktopDpi(IntPtr hDesktop)
        {
            using (var desktopDc = User32.GetDC(hDesktop))
            {
                if (desktopDc == null || desktopDc.IsInvalid)
                {
                    var error = Kernel32.GetLastError();
                    Log.Warn($"Failed to GetDC for desktop {hDesktop.ToInt64()}, error: {error}");
                    return PointF.Empty;
                }

                using (var graphics = Graphics.FromHdc(desktopDc.DangerousGetHandle()))
                {
                    return GetDpi(graphics);
                }
            }
        }

        private static PointF GetDpi(Graphics graphics)
        {
            return new(graphics.DpiX / 96f, graphics.DpiY / 96f);
        }

        public static Rectangle GetClientRect(IntPtr hwnd)
        {
            var rect = new RECT();
            if (!User32.GetClientRect(hwnd, out rect))
            {
                Log.Warn($"Failed to GetClientRect({hwnd}), LastError: {Kernel32.GetLastError()}");
                return Rectangle.Empty;
            }

            var windowBounds = GetWindowRect(hwnd);
            var clientSize = new Size(rect.right - rect.left, rect.bottom - rect.top);

            return new Rectangle(
                windowBounds.Left,
                windowBounds.Top,
                clientSize.Width,
                clientSize.Height);
        }

        public static bool IsWindowsXP()
        {
            return OSVersion.Major == 5 && OSVersion.Minor == 1;
        }

        public static bool IsWindowsXPOrGreater()
        {
            return OSVersion.Major == 5 && OSVersion.Minor >= 1 || OSVersion.Major > 5;
        }

        public static bool IsWindowsVista()
        {
            return OSVersion.Major == 6;
        }

        public static bool IsWindowsVistaOrGreater()
        {
            return OSVersion.Major >= 6;
        }

        public static bool IsWindows7()
        {
            return OSVersion.Major == 6 && OSVersion.Minor == 1;
        }

        public static bool IsWindows7OrGreater()
        {
            return OSVersion.Major == 6 && OSVersion.Minor >= 1 || OSVersion.Major > 6;
        }

        public static bool IsWindows8()
        {
            return OSVersion.Major == 6 && OSVersion.Minor == 2;
        }

        public static bool IsWindows8OrGreater()
        {
            return OSVersion.Major == 6 && OSVersion.Minor >= 2 || OSVersion.Major > 6;
        }

        public static bool IsWindows10OrGreater(int build = -1)
        {
            return OSVersion.Major >= 10 && OSVersion.Build >= build;
        }

        public static IntPtr GetDesktopWindow()
        {
            return User32.GetDesktopWindow();
        }

        public static bool SetForegroundWindow(IntPtr hwnd)
        {
            var log = Log.WithSuffix($"[{hwnd.ToHexadecimal()}]");
            log.Debug(() => $"Setting foreground window");

            var foregroundWindow = GetForegroundWindow();
            if (hwnd == foregroundWindow)
            {
                log.Debug(() => $"Window is already foreground");
                return true;
            }

            using var attachmentAnchor = UserInputAttacher.Attach(log, hwnd);
            log.Debug(() => $"Requesting window activation");
            Win32ErrorCode error;

            if (!BringWindowToTop(hwnd) && (error = Kernel32.GetLastError()) != Win32ErrorCode.NERR_Success)
            {
                log.Warn($"Failed to SetForegroundWindow.BringWindowToTop({hwnd.ToHexadecimal()}), error: {error}");
                return false;
            }

            if (!User32.SetForegroundWindow(hwnd))
            {
                log.Warn($"Failed to SetForegroundWindow({hwnd.ToHexadecimal()})");
                return false;
            }

            return true;
        }

        private sealed class UserInputAttacher : DisposableReactiveObject
        {
            public static IDisposable Attach(IFluentLog log, IntPtr hwnd)
            {
                var targetThreadId = User32.GetWindowThreadProcessId(hwnd, out var targetProcessId);
                if (targetThreadId <= 0)
                {
                    log.Warn(
                        $"Failed to retrieve foreground thread of process {targetProcessId} for window {hwnd.ToHexadecimal()} ({GetWindowTitle(hwnd)}), last error: {Kernel32.GetLastError()}");
                    return Disposable.Empty;
                }
                
                var appThread = GetCurrentThreadId();
                if (targetThreadId == appThread)
                {
                    Log.Debug(() => $"Attachment is not needed - we're already on thread {targetThreadId}");
                    return Disposable.Empty;
                }
                
                Log.Debug(() => $"Attaching thread input of thread {appThread} to thread {targetThreadId} of process {targetProcessId}");
                User32.AttachThreadInput(appThread, targetThreadId, true);

                return Disposable.Create(() =>
                {
                    if (targetThreadId != appThread)
                    {
                        log.Debug(() => $"Detaching thread input of thread {appThread} to thread {targetThreadId} of process {targetProcessId}");
                        User32.AttachThreadInput(appThread, targetThreadId, false);
                    }
                });
            }
        }
    }
}