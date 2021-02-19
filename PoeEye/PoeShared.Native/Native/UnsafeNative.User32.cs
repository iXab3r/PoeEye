using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using PInvoke;
using PoeShared.Scaffolding;

namespace PoeShared.Native
{
    public partial class UnsafeNative
    {
        [DllImport("user32.dll")]
        private static extern bool AllowSetForegroundWindow(int processId);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetKeyNameText(int lParam, [MarshalAs(UnmanagedType.LPWStr)] [Out] StringBuilder str, int size);
        
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
        
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool AdjustWindowRectEx(ref RECT lpRect, User32.WindowStyles dwStyle, bool bMenu, User32.WindowStylesEx dwExStyle); 
        
        
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool AdjustWindowRectExForDpi(ref RECT lpRect, User32.WindowStyles dwStyle, bool bMenu, User32.WindowStylesEx dwExStyle, int dpi); 
        
        [DllImport("user32.dll")]
        static extern bool SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);
        
        [DllImport("user32.dll", SetLastError=true)]
        static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError=true)]
        static extern bool BringWindowToTop(HandleRef hWnd);
        
        [DllImport("gdi32.dll")]
        static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2,int cx, int cy);
        
        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPOS
        {
            public readonly IntPtr hwnd;
            public readonly IntPtr hwndInsertAfter;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public readonly int flags;
        }
        
        /// <summary>
        ///     Indicates whether various virtual keys are down. This parameter can be one or more of the following values.
        ///     https://docs.microsoft.com/en-us/windows/win32/inputdev/wm-lbuttonup
        /// </summary>
        [Flags]
        public enum WmMouseParam
        {
            NONE = 0x0000,
            MK_LBUTTON = 0x0001,
            MK_RBUTTON = 0x0002,
            MK_SHIFT = 0x0004,
            MK_CONTROL = 0x0008,
            MK_MBUTTON = 0x0010,
            MK_XBUTTON1 = 0x0020,
            MK_XBUTTON2 = 0x0040,
        }
        
        public static string GetWindowClass(IntPtr hwnd)
        {
            try
            {
                return User32.GetClassName(hwnd);
            }
            catch (Win32Exception e)
            {
                Log.Warn($"Failed to get GetWindowClass({hwnd.ToHexadecimal()}) - {e}");
                return null;
            }
        }
        
        public static Point GetMousePosition() 
        {
            User32.GetCursorPos(out var pt);
            return new Point(pt.x, pt.y);
        }
        
        public static string GetWindowTitle(IntPtr hwnd)
        {
            try
            {
                const int nChars = 256;
                var buff = new StringBuilder(nChars);

                return  GetWindowText(hwnd, buff, nChars) > 0
                    ? buff.ToString()
                    : null;
            }
            catch (Win32Exception e)
            {
                Log.Warn($"Failed to get GetWindowText({hwnd.ToHexadecimal()}) - {e}");
                return null;
            }
        }

        public static int GetProcessIdByWindowHandle(IntPtr hwnd)
        {
            User32.GetWindowThreadProcessId(hwnd, out var processId);
            return processId;
        }
        
        public static bool SetWindowRgn(IntPtr hwnd, Rectangle rect)
        {
            var hRect = CreateRoundRectRgn(rect.X, rect.Y, rect.X + rect.Width, rect.Y + rect.Height, 0, 0);
            if (hRect == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            return SetWindowRgn(hwnd, hRect, bRedraw: true);
        }
    }
}