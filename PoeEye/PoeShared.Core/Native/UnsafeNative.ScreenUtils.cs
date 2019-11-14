using System;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using PInvoke;
using PoeShared.Scaffolding;

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
                }.DumpToTextRaw();
            }
        }

        public static PointF GetDesktopDpi()
        {
            var desktopWindow = User32.GetDesktopWindow();
            using (var desktopDc = User32.GetDC(desktopWindow))
            {
                if (desktopDc.IsInvalid)
                {
                    var error = Kernel32.GetLastError();
                    Log.Warn($"Failed to GetDC for desktop {desktopWindow.ToInt64()}, error: {error}");
                    return PointF.Empty;
                }
                var graphics = Graphics.FromHdc(desktopDc.DangerousGetHandle());
                return GetDpi(graphics);
            }
        }
        
        private static PointF GetDpi(Graphics graphics)
        {
            return new PointF(graphics.DpiX / 96f, graphics.DpiY / 96f);
        }

        public static Rectangle GetClientRect(IntPtr hwnd)
        {
            var rect = new RECT();
            if (!User32.GetClientRect(hwnd, out rect))
            {
                Log.Warn($"Failed to GetClientRect({hwnd}), LastError: {Kernel32.GetLastError()}");
            }
            return new Rectangle(rect.left, rect.top, rect.bottom - rect.top, rect.right - rect.left);
        }
        
        public static bool IsWindowsXP()
        {
            return OSVersion.Major == 5 && OSVersion.Minor == 1;
        }

        public static bool IsWindowsXPOrGreater()
        {
            return (OSVersion.Major == 5 && OSVersion.Minor >= 1) || OSVersion.Major > 5;
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
            return (OSVersion.Major == 6 && OSVersion.Minor >= 1) || OSVersion.Major > 6;
        }

        public static bool IsWindows8()
        {
            return OSVersion.Major == 6 && OSVersion.Minor == 2;
        }

        public static bool IsWindows8OrGreater()
        {
            return (OSVersion.Major == 6 && OSVersion.Minor >= 2) || OSVersion.Major > 6;
        }

        public static bool IsWindows10OrGreater(int build = -1)
        {
            return OSVersion.Major >= 10 && OSVersion.Build >= build;
        }
        
        public static IntPtr GetDesktopWindow()
        {
            return User32.GetDesktopWindow();
        }

        public static bool SetForegroundWindow(in IntPtr windowHandle)
        {
            return User32.SetForegroundWindow(windowHandle);
        }
    }
}