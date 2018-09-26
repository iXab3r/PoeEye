using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Common.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.Native
{
    public static class WindowsServices
    {
        private const int WS_EX_LAYERED = 0x00080000;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int GWL_EXSTYLE = -20;
        private const int SW_SHOWNOACTIVATE = 4;
        private const int SW_HIDE = 0;
        private const int SW_SHOWNORMAL = 0;
        private const int HWND_TOPMOST = -1;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const int SWP_NOMOVE = 0x0002;
        private const int SWP_NOSIZE = 0x0001;
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private static readonly ILog Log = LogManager.GetLogger(typeof(WindowsServices));

        static WindowsServices()
        {
            try
            {
                var processId = Process.GetCurrentProcess().Id;
                Log.Debug($"Calling AllowSetForegroundWindow(pid: {processId})");
                var result = AllowSetForegroundWindow((uint)processId);
                if (!result)
                {
                    Log.Warn("AllowSetForegroundWindow has failed !");
                }
                else
                {
                    Log.Debug($"Successfully executed AllowSetForegroundWindow(pid: {processId})");
                }
            }
            catch (Exception e)
            {
                Log.HandleException(e);
            }
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        private static extern bool SetWindowPos(
            IntPtr hWnd, // Window handle
            int hWndInsertAfter, // Placement-order handle
            int X, // Horizontal position
            int Y, // Vertical position
            int cx, // Width
            int cy, // Height
            uint uFlags); // Window positioning flags

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern bool AllowSetForegroundWindow(uint processId);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        public static extern int GetDpiForWindow(IntPtr hWnd);

        [DllImport("User32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
        
        public static float GetDisplayScaleFactor(IntPtr windowHandle)
        {
            try
            {
                return GetDpiForWindow(windowHandle) / 96f;
            }
            catch
            {
                // Or fallback to GDI solutions above
                return 1;
            }
        }

        public static void HideSystemMenu(IntPtr hwnd)
        {
            Log.Trace($"[{hwnd.ToHexadecimal()}] Hiding SystemMenu");

            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }

        public static void ShowInactiveTopmost(IntPtr handle, int left, int top, int width, int height)
        {
            var dpi = GetDisplayScaleFactor(handle);
            Log.Trace($"[{handle.ToHexadecimal()}] Showing window X:{left} Y:{top} Width:{width} Height:{height}, scaleFactor: {dpi}");
            ShowWindow(handle, SW_SHOWNOACTIVATE);

            SetWindowPos(handle, HWND_TOPMOST, left, top, (int)(width * dpi), (int)(height * dpi), SWP_NOACTIVATE);
        }
        
        public static void ShowWindow(IntPtr handle)
        {
            Log.Trace($"[{handle.ToHexadecimal()}] Showing window");

            ShowWindow(handle, SW_SHOWNORMAL);
        }

        public static void HideWindow(IntPtr handle)
        {
            Log.Trace($"[{handle.ToHexadecimal()}] Hiding window");

            ShowWindow(handle, SW_HIDE);
        }

        public static void SetWindowExTransparent(IntPtr hwnd)
        {
            Log.Trace($"[{hwnd.ToHexadecimal()}] Reconfiguring window to Transparent");

            var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            extendedStyle &= ~WS_EX_LAYERED;
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TOOLWINDOW | WS_EX_TRANSPARENT);
        }

        public static void SetWindowExLayered(IntPtr hwnd)
        {
            Log.Trace($"[{hwnd.ToHexadecimal()}] Reconfiguring window to Layered");

            var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            extendedStyle &= ~WS_EX_TRANSPARENT;
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TOOLWINDOW | WS_EX_LAYERED);
        }

        public static void SetWindowExNoActivate(IntPtr hwnd)
        {
            Log.Trace($"[{hwnd.ToHexadecimal()}] Reconfiguring window to NoActivate");

            var existingStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, existingStyle | WS_EX_NOACTIVATE);
        }
    }
}