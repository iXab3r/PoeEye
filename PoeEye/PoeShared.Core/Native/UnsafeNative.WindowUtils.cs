using System;
using System.Drawing;
using PInvoke;
using PoeShared.Scaffolding;

namespace PoeShared.Native
{
    public partial class UnsafeNative
    {
        /// <summary>
        /// Gets the z-order for one or more windows atomically with respect to each other. In Windows, smaller z-order is higher. If the window is not top level, the z order is returned as -1. 
        /// </summary>
        public static int[] GetZOrder(params IntPtr[] hWnds)
        {
            var z = new int[hWnds.Length];
            for (var i = 0; i < hWnds.Length; i++) z[i] = -1;

            var index = 0;
            var numRemaining = hWnds.Length;
            User32.EnumWindows((wnd, param) =>
            {
                var searchIndex = Array.IndexOf(hWnds, wnd);
                if (searchIndex != -1)
                {
                    z[searchIndex] = index;
                    numRemaining--;
                    if (numRemaining == 0) return false;
                }
                index++;
                return true;
            }, IntPtr.Zero);

            return z;
        }

        public static Rectangle GetWindowRect(IntPtr hwnd)
        {
            if (!User32.GetWindowRect(hwnd, out var result))
            {
                Log.Warn($"Failed to get size of Window by HWND {hwnd.ToHexadecimal()}");
                return Rectangle.Empty;
            }

            return new Rectangle(result.left, result.top, result.right - result.left, result.bottom - result.top);
        }

        public static bool SetWindowRect(IntPtr hwnd, Rectangle rect)
        {
            Log.Debug($"[{hwnd.ToHexadecimal()}] Setting window bounds: {rect}");
            
            if (!User32.SetWindowPos(hwnd, User32.SpecialWindowHandles.HWND_TOP, rect.X, rect.Y, rect.Width, rect.Height, User32.SetWindowPosFlags.SWP_NOACTIVATE))
            {
                Log.Warn($"Failed to SetWindowPos({hwnd.ToHexadecimal()}, {User32.SpecialWindowHandles.HWND_TOPMOST}), error: {Kernel32.GetLastError()}");
                return false;
            }
            return true;
        }

        public static void HideSystemMenu(IntPtr hwnd)
        {
            Log.Debug($"[{hwnd.ToHexadecimal()}] Hiding SystemMenu");

            var existingStyle = (User32.SetWindowLongFlags)User32.GetWindowLong(hwnd, User32.WindowLongIndexFlags.GWL_STYLE);
            var newStyle = existingStyle & ~User32.SetWindowLongFlags.WS_SYSMENU;
            if (User32.SetWindowLong(hwnd, User32.WindowLongIndexFlags.GWL_STYLE, newStyle) == 0)
            {
                Log.Warn($"Failed to SetWindowLong to {newStyle} (previously {existingStyle}) of Window by HWND {hwnd.ToHexadecimal()}");
            }
        }
        
        public static bool SetWindowExTransparent(IntPtr hwnd)
        {
            Log.Debug($"[{hwnd.ToHexadecimal()}] Reconfiguring window to Transparent");

            var existingStyle = (User32.SetWindowLongFlags)User32.GetWindowLong(hwnd, User32.WindowLongIndexFlags.GWL_EXSTYLE);
            var newStyle = existingStyle;
            newStyle &= ~User32.SetWindowLongFlags.WS_EX_LAYERED;
            newStyle |= User32.SetWindowLongFlags.WS_EX_TOOLWINDOW;
            newStyle |= User32.SetWindowLongFlags.WS_EX_TRANSPARENT;
            if (User32.SetWindowLong(hwnd, User32.WindowLongIndexFlags.GWL_EXSTYLE, newStyle) == 0)
            {
                Log.Warn($"Failed to SetWindowLong to {newStyle} (previously {existingStyle}) of Window by HWND {hwnd.ToHexadecimal()}");
                return false;
            }

            return true;
        }

        public static bool SetWindowExLayered(IntPtr hwnd)
        {
            Log.Debug($"[{hwnd.ToHexadecimal()}] Reconfiguring window to Layered");

            var existingStyle = (User32.SetWindowLongFlags)User32.GetWindowLong(hwnd, User32.WindowLongIndexFlags.GWL_EXSTYLE);
            var newStyle = existingStyle;
            newStyle &= ~User32.SetWindowLongFlags.WS_EX_TRANSPARENT;
            newStyle |= User32.SetWindowLongFlags.WS_EX_TOOLWINDOW;
            newStyle |= User32.SetWindowLongFlags.WS_EX_LAYERED;
            if (User32.SetWindowLong(hwnd, User32.WindowLongIndexFlags.GWL_EXSTYLE, newStyle) == 0)
            {
                Log.Warn($"Failed to SetWindowLong to {newStyle} (previously {existingStyle}) of Window by HWND {hwnd.ToHexadecimal()}");
                return false;
            }
            return true;
        }

        public static bool SetWindowExNoActivate(IntPtr hwnd)
        {
            Log.Debug($"[{hwnd.ToHexadecimal()}] Reconfiguring window to NoActivate");
            var existingStyle = (User32.SetWindowLongFlags)User32.GetWindowLong(hwnd, User32.WindowLongIndexFlags.GWL_EXSTYLE);
            var newStyle = existingStyle;
            newStyle |= User32.SetWindowLongFlags.WS_EX_NOACTIVATE;
            if (User32.SetWindowLong(hwnd, User32.WindowLongIndexFlags.GWL_EXSTYLE, newStyle) == 0)
            {
                Log.Warn($"Failed to SetWindowLong to {newStyle} (previously {existingStyle}) of Window by HWND {hwnd.ToHexadecimal()}");
                return false;
            }

            return true;
        }

        public static void ShowInactiveTopmost(IntPtr handle, int left, int top, int width, int height)
        {
            var dpi = GetDisplayScaleFactor(handle);
            Log.Debug($"[{handle.ToHexadecimal()}] Showing window X:{left} Y:{top} Width:{width} Height:{height}, scaleFactor: {dpi}");
            if (!User32.ShowWindow(handle, User32.WindowShowStyle.SW_SHOWNOACTIVATE))
            {
                Log.Warn($"Failed to ShowWindow({handle.ToHexadecimal()}), error: {Kernel32.GetLastError()}");
                return;
            }
            
            var rect = new Rectangle(left, top, (int) (width * dpi), (int) (height * dpi));
            if (!User32.SetWindowPos(handle, User32.SpecialWindowHandles.HWND_TOPMOST, rect.X, rect.Y, rect.Width, rect.Height, User32.SetWindowPosFlags.SWP_NOACTIVATE))
            {
                Log.Warn($"Failed to SetWindowPos({handle.ToHexadecimal()}, {User32.SpecialWindowHandles.HWND_TOPMOST}), error: {Kernel32.GetLastError()}");
            }
        }

        public static bool ShowWindow(IntPtr handle)
        {
            Log.Debug($"[{handle.ToHexadecimal()}] Showing window");
            if (!User32.ShowWindow(handle, User32.WindowShowStyle.SW_SHOWNORMAL))
            {
                Log.Warn($"Failed to ShowWindow({handle.ToHexadecimal()}), error: {Kernel32.GetLastError()}");
                return false;
            }

            return true;
        }

        public static bool HideWindow(IntPtr handle)
        {
            Log.Debug($"[{handle.ToHexadecimal()}] Hiding window");
            
            if (!User32.ShowWindow(handle, User32.WindowShowStyle.SW_HIDE))
            {
                Log.Warn($"Failed to HideWindow({handle.ToHexadecimal()}), error: {Kernel32.GetLastError()}");
                return false;
            }
            return true;
        }
        
        public static float GetDisplayScaleFactor(IntPtr hwnd)
        {
            try
            {
                return User32.GetDpiForWindow(hwnd)  / 96f;
            }
            catch
            {
                // Or fallback to GDI solutions above
                return 1;
            }
        }

        public static IntPtr GetForegroundWindow()
        {
            return User32.GetForegroundWindow();
        }
    }
}