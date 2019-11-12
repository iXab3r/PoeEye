using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using log4net;
using PoeShared.Scaffolding;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace PoeShared.Native
{
    public static class UnsafeNative
    {
        public delegate void WinEventDelegate(
            IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread,
            uint dwmsEventTime);

        private static readonly ILog Log = LogManager.GetLogger(typeof(UnsafeNative));

        [DllImport("user32.dll")]
        public static extern int MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetKeyNameText(int lParam, [MarshalAs(UnmanagedType.LPWStr)] [Out] StringBuilder str, int size);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern void SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc,
            WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        public static extern bool UnhookWinEvent(IntPtr hHook);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindow(string strClassName, string strWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);

        [DllImport("user32.dll", EntryPoint = "GetDC")]
        private static extern IntPtr GetDC(IntPtr ptr);
        
        [DllImport("user32.dll")]
        public static extern IntPtr GetDesktopWindow();
        
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPos(ref Win32Point pt);
        
        [StructLayout(LayoutKind.Sequential)]
        public struct Win32Point
        {
            public int X;
            public int Y;
        }
        
        public static Point GetMousePosition() // mouse position relative to screen
        {
            var w32Mouse = new UnsafeNative.Win32Point();
            UnsafeNative.GetCursorPos(ref w32Mouse);
            return new Point(w32Mouse.X, w32Mouse.Y);
        }
        
        public static bool IsElevated()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        public static string GetWindowTitle(IntPtr hwnd)
        {
            const int nChars = 256;
            var buff = new StringBuilder(nChars);

            return GetWindowText(hwnd, buff, nChars) > 0
                ? buff.ToString()
                : null;
        }

        public static uint GetProcessIdByWindowHandle(IntPtr hwnd)
        {
            GetWindowThreadProcessId(hwnd, out var processId);
            return processId;
        }

        public static Rectangle GetWindowRect(IntPtr hwnd)
        {
            var result = new Rect();
            if (!GetWindowRect(hwnd, ref result))
            {
                Log.Warn($"Failed to get size of Window by HWND {hwnd.ToInt64():x8}");
                return Rectangle.Empty;
            }

            return new Rectangle((int) result.X, (int) result.Y, (int) result.Width, (int) result.Height);
        }
        
        public static Rect GetActiveMonitorBounds(Window window)
        {
            var handle = window != null
                ? new WindowInteropHelper(window).Handle
                : IntPtr.Zero;
            return GetActiveMonitorBounds(handle);
        }

        public static Rect GetActiveMonitorBounds(IntPtr windowHandle)
        {
            var screen = Screen.FromHandle(windowHandle);
            var graphics = Graphics.FromHdc(GetDC(windowHandle));
            var dpi = GetDpi(graphics);

            Log.Debug($"Monitor for window 0x{windowHandle.ToInt64():X8}: {GetMonitorInfo(windowHandle)}");
            return GetMonitorBounds(screen, dpi);
        }

        public static Rect GetMonitorBounds(Screen monitor, PointF dpi)
        {
            var result = new Rect(
                monitor.Bounds.X,
                monitor.Bounds.Y,
                monitor.Bounds.Width,
                monitor.Bounds.Height);
            result.Scale(1 / dpi.X, 1 / dpi.Y);
            return result;
        }

        public static PointF GetDesktopDpi()
        {
            var windowHandle = GetDesktopWindow();
            var graphics = Graphics.FromHdc(GetDC(windowHandle));
            
            return GetDpi(graphics);
        }

        private static PointF GetDpi(Graphics graphics)
        {
            return new PointF(graphics.DpiX / 96f, graphics.DpiY / 96f);
        }
        
        public static bool IsOutOfBounds(Point point, Size bounds)
        {
            return IsOutOfBounds(point, new Rect(new Point(), bounds));
        }
        
        public static bool IsOutOfBounds(Rect frame, Rect bounds)
        {
            var downscaledFrame = frame;
            // downscaling frame as we do not require for FULL frame to be visible, only top-left part of it
            downscaledFrame.Size = frame.Size.Scale(0.5);
            
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
            screenCenter.Offset(- windowSize.Width / 2, - windowSize.Height / 2);

            return screenCenter;
        }

        public static Point GetPositionAtTheCenter(Window window)
        {
            var monitorBounds = GetActiveMonitorBounds(window);

            return GetPositionAtTheCenter(monitorBounds, new Size(window.Width, window.Height));
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
            return GetMonitorInfo(GetDesktopWindow());
        }

        public static string GetMonitorInfo(IntPtr windowHandle)
        {
            var screen = Screen.FromHandle(windowHandle);
            var graphics = Graphics.FromHdc(GetDC(windowHandle));
            var scaledBounds = GetMonitorBounds(screen, new PointF(graphics.DpiX, graphics.DpiY));
            return new
            {
                screen.DeviceName, screen.Primary, graphics.PageScale, SystemBounds = screen.Bounds, ScaledBounds = scaledBounds, graphics.DpiX, graphics.DpiY
            }.DumpToTextRaw();
        }

        public static class Constants
        {
            [Flags]
            public enum RedrawWindowFlags : uint
            {
                /// <summary>
                ///     Invalidates the rectangle or region that you specify in lprcUpdate or hrgnUpdate.
                ///     You can set only one of these parameters to a non-NULL value. If both are NULL, RDW_INVALIDATE invalidates the
                ///     entire window.
                /// </summary>
                Invalidate = 0x1,

                /// <summary>
                ///     Causes the OS to post a WM_PAINT message to the window regardless of whether a portion of the window is
                ///     invalid.
                /// </summary>
                InternalPaint = 0x2,

                /// <summary>
                ///     Causes the window to receive a WM_ERASEBKGND message when the window is repainted.
                ///     Specify this value in combination with the RDW_INVALIDATE value; otherwise, RDW_ERASE has no effect.
                /// </summary>
                Erase = 0x4,

                /// <summary>
                ///     Validates the rectangle or region that you specify in lprcUpdate or hrgnUpdate.
                ///     You can set only one of these parameters to a non-NULL value. If both are NULL, RDW_VALIDATE validates the entire
                ///     window.
                ///     This value does not affect internal WM_PAINT messages.
                /// </summary>
                Validate = 0x8,

                NoInternalPaint = 0x10,

                /// <summary>Suppresses any pending WM_ERASEBKGND messages.</summary>
                NoErase = 0x20,

                /// <summary>Excludes child windows, if any, from the repainting operation.</summary>
                NoChildren = 0x40,

                /// <summary>Includes child windows, if any, in the repainting operation.</summary>
                AllChildren = 0x80,

                /// <summary>
                ///     Causes the affected windows, which you specify by setting the RDW_ALLCHILDREN and RDW_NOCHILDREN values, to receive
                ///     WM_ERASEBKGND and WM_PAINT
                ///     messages before the RedrawWindow returns, if necessary.
                /// </summary>
                UpdateNow = 0x100,

                /// <summary>
                ///     Causes the affected windows, which you specify by setting the RDW_ALLCHILDREN and RDW_NOCHILDREN values, to receive
                ///     WM_ERASEBKGND messages before
                ///     RedrawWindow returns, if necessary.
                ///     The affected windows receive WM_PAINT messages at the ordinary time.
                /// </summary>
                EraseNow = 0x200,

                Frame = 0x400,

                NoFrame = 0x800
            }

            public const uint WINEVENT_OUTOFCONTEXT = 0;
            public const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B;
            public const int EVENT_SYSTEM_FOREGROUND = 3;

            public const int GCLP_HBRBACKGROUND = -0x0A;

            public const uint TPM_RETURNCMD = 0x0100;
            public const uint TPM_LEFTBUTTON = 0x0;

            public const uint SYSCOMMAND = 0x0112;

            public const int MF_GRAYED = 0x00000001;
            public const int MF_BYCOMMAND = 0x00000000;
            public const int MF_ENABLED = 0x00000000;

            public const int WM_HOTKEY = 0x0312;

            public const int VK_SHIFT = 0x10;
            public const int VK_CONTROL = 0x11;
            public const int VK_MENU = 0x12;

            /* used by UnsafeNativeMethods.MapVirtualKey */
            public const uint MAPVK_VK_TO_VSC = 0x00;
            public const uint MAPVK_VSC_TO_VK = 0x01;
            public const uint MAPVK_VK_TO_CHAR = 0x02;
            public const uint MAPVK_VSC_TO_VK_EX = 0x03;
            public const uint MAPVK_VK_TO_VSC_EX = 0x04;

            /// <summary>
            ///     Causes the dialog box to display all available colors in the set of basic colors.
            /// </summary>
            public const int CC_ANYCOLOR = 0x00000100;
            /* used by UnsafeNativeMethods.MapVirtualKey (end) */

            public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
            public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
            public static readonly IntPtr HWND_TOP = new IntPtr(0);
            public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        }
    }
}