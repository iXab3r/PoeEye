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
    }
}