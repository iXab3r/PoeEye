using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using PInvoke;
using PoeShared.Scaffolding; 
using PoeShared.Logging;

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
        static extern int GetKeyboardLayoutList(int nBuff, [Out] IntPtr[] lpList);
        
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr ActivateKeyboardLayout(IntPtr hkl, uint flags);
        
        [DllImport("user32.dll")]
        static extern bool GetKeyboardLayoutName([Out] StringBuilder klId);
        
        [DllImport("user32.dll")]
        static extern IntPtr GetKeyboardLayout(uint idThread);
        
        [DllImport("user32.dll")]
        static extern bool SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);
        
        [DllImport("user32.dll", SetLastError=true)]
        static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError=true)]
        static extern bool BringWindowToTop(HandleRef hWnd);
        
        [DllImport("gdi32.dll")]
        static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2,int cx, int cy);
        
        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern int GetWindowLong32(IntPtr hWnd, WindowLong nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, WindowLong nIndex);

        [DllImport("user32.dll")]
        public static extern IntPtr GetMenu(IntPtr hwnd);

        [DllImport("user32.dll", EntryPoint = "GetClassLongPtrW")]
        private static extern IntPtr GetClassLong64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetClassLongW")]
        private static extern int GetClassLong32(IntPtr hWnd, int nIndex);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("User32", CharSet = CharSet.Auto)]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndParent);
        
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

        [Flags]
        public enum WindowExStyles : long
        {
            AppWindow = 0x40000,
            ToolWindow = 0x80,
        }

        public enum WindowLong
        {
            ExStyle = -20,
        }

        public enum ClassLong
        {
            Icon = -14,
            IconSmall = -34
        }
        
        /// <summary>
        ///     Checks whether a window is a top-level window (has no owner nor parent window).
        /// </summary>
        /// <param name="hwnd">Handle to the window to check.</param>
        public static bool IsTopLevel(IntPtr hwnd)
        {
            var hasParent = UnsafeNative.GetParent(hwnd).ToInt64() != 0;
            var hasOwner = User32.GetWindow(hwnd, User32.GetWindowCommands.GW_OWNER).ToInt64() != 0;

            return !hasParent && !hasOwner;
        }
        
        public static IntPtr GetClassLong(IntPtr hWnd, ClassLong i)
        {
            if (IntPtr.Size == 8)
            {
                return GetClassLong64(hWnd, (int) i);
            }

            return new IntPtr(GetClassLong32(hWnd, (int) i));
        }
        
        public static IntPtr GetWindowLong(IntPtr hWnd, WindowLong i)
        {
            if (IntPtr.Size == 8)
            {
                return GetWindowLongPtr64(hWnd, i);
            }

            return new IntPtr(GetWindowLong32(hWnd, i));
        }

        public static IntPtr WindowFromPoint(Point point)
        {
            var pt = new POINT()
            {
                x = point.X,
                y = point.Y
            };
            return User32.WindowFromPoint(pt);
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

        /// <summary>
        /// Retrieves the input locale identifiers (formerly called keyboard layout handles) corresponding to the current set of input locales in the system.
        /// </summary>
        /// <returns></returns>
        public static IntPtr[] GetKeyboardLayoutList()
        {
            var buffer = new IntPtr[256];
            var itemCount = GetKeyboardLayoutList(buffer.Length, buffer);
            return buffer.Take(itemCount).Where(x => x != IntPtr.Zero).ToArray();
        }

        public static string GetActiveKeyboardLayoutName()
        {
            var currentKeyboardLayout = GetKeyboardLayout(0);
            return GetKeyboardLayoutName(currentKeyboardLayout);
        }
        
        public static string GetKeyboardLayoutName(IntPtr hkl)
        {
            var currentKeyboardLayout = GetKeyboardLayout(0);
            try
            {
                var previous = ActivateKeyboardLayout(hkl, 0);
                if (previous == IntPtr.Zero)
                {
                    throw new Win32Exception(Kernel32.GetLastError(), $"Failed to activate KHL {hkl.ToHexadecimal()}, got {previous.ToHexadecimal()}");
                };
                
                var result = new StringBuilder(256);
                if (!GetKeyboardLayoutName(result))
                {
                    throw new Win32Exception(Kernel32.GetLastError(), $"Failed to GetKeyboardLayoutName");
                }
                return result.ToString();
            }
            finally
            {
                ActivateKeyboardLayout(currentKeyboardLayout, 0);
            }
        }
    }
}