using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Interop;
using NAudio.Utils;
using PInvoke;
using PoeShared.Scaffolding;
using PoeShared.Logging;
using HResult = PInvoke.HResult;

namespace PoeShared.Native;

public partial class UnsafeNative
{
    public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
    public static readonly IntPtr HWND_TOP = new IntPtr(0);
    public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
    
    /// <summary>
    /// Specifies the method which will be used when calling ActivateWindow
    /// </summary>
    public static UnsafeWindowActivationMethod WindowActivationMethod { get; set; } = UnsafeWindowActivationMethod.Auto;

    /// <summary>
    /// In some cases GetForegroundWindow returns NULL, that means that "future" foreground window is still activating
    /// If system returns NULL we want to give a chance for window to activate. This is max timeout we'll wait for to GetForegroundWindow to return non-null value
    /// </summary>
    private static readonly TimeSpan MaxWindowActivationTimeout = TimeSpan.FromSeconds(10);

    private static readonly TimeSpan MinWindowActivationTimeout = TimeSpan.FromMilliseconds(10);
    
    [StructLayout(LayoutKind.Sequential)]
    // ReSharper disable once InconsistentNaming
    public struct TITLEBARINFO
    {
        public const int CCHILDREN_TITLEBAR = 5;
        public uint cbSize;
        public RECT rcTitleBar;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = CCHILDREN_TITLEBAR + 1)]
        public uint[] rgstate;
    }

    [DllImport("dwmapi.dll")]
    private static extern HResult DwmGetWindowAttribute(IntPtr hwnd, DwmApi.DWMWINDOWATTRIBUTE dwAttribute, out RECT pvAttribute, int cbAttribute);

    [DllImport("dwmapi.dll")]
    private static extern HResult DwmGetWindowAttribute(IntPtr hwnd, DwmApi.DWMWINDOWATTRIBUTE dwAttribute, out bool pvAttribute, int cbAttribute);
    
    [DllImport("dwmapi.dll", PreserveSig = false)]
    public static extern bool DwmIsCompositionEnabled();
    
    [DllImport("user32.dll")]
    private static extern bool GetTitleBarInfo(IntPtr hwnd, ref TITLEBARINFO pti);
    
    
    /// <summary>Determines whether a window is maximized.</summary>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsZoomed(IntPtr hWnd);

    public static WinRect GetTitleBarRect(IntPtr hwnd)
    {
        var titleBarInfo = GetTitleBarInformation(hwnd);
        return Rectangle.FromLTRB(titleBarInfo.rcTitleBar.left, titleBarInfo.rcTitleBar.top, titleBarInfo.rcTitleBar.right, titleBarInfo.rcTitleBar.bottom);
    }
    
    public static RECT AdjustWindowRectExForDpi(IntPtr hwnd)
    {
        var dpi = User32.GetDpiForWindow(hwnd);
        var rect = new RECT();
        if (!AdjustWindowRectExForDpi(ref rect, User32.WindowStyles.WS_OVERLAPPEDWINDOW, false, 0, dpi))
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }

        return rect;
    }
    
    public static TITLEBARINFO GetTitleBarInformation(IntPtr hwnd)
    {
        var tbi = new TITLEBARINFO
        {
            cbSize = (uint)Marshal.SizeOf(typeof(TITLEBARINFO))
        };

        if (!GetTitleBarInfo(hwnd, ref tbi))
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }

        return tbi;
    }
    
    public static IntPtr MakeLParam(int loWord, int hiWord)
    {
        return new IntPtr((hiWord << 16) | (loWord & 0xffff));
    }
    
    public static Rectangle RetrieveWindowRectangle(IntPtr handle)
    {
        var rect = Rectangle.Empty;
        if (IsDWMEnabled() && TryGetDwmWindowFrameBounds(handle, out var dwmRect))
        {
            rect = dwmRect;
        }

        if (rect.IsEmpty)
        {
            rect = GetWindowRect(handle);
        }

        return rect;
    }

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

    public static Rectangle DwmGetWindowFrameBoundsWithinMonitor(IntPtr hwnd)
    {
        var windowRect = DwmGetWindowFrameBounds(hwnd);
        var monitorRect = System.Windows.Forms.Screen.FromHandle(hwnd).Bounds;
        return windowRect with {X = windowRect.X - monitorRect.X, Y = windowRect.Y - monitorRect.Y};
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

    public static bool DwmGetWindowAttribute(IntPtr hwnd, DwmApi.DWMWINDOWATTRIBUTE flags)
    {
        var result = DwmGetWindowAttribute(hwnd, flags, out bool resultValue, Marshal.SizeOf<bool>());
        if (!result.Succeeded)
        {
            Log.Warn($"Failed to DwmGetWindowAttribute by HWND {hwnd.ToHexadecimal()}, error: {result}");
            throw new Win32Exception();
        }

        return resultValue;
    }

    public static bool TryGetDwmWindowFrameBounds(IntPtr hwnd, out Rectangle bounds)
    {
        if (!IsDWMEnabled())
        {
            bounds = default;
            return false;
        }

        var result = DwmGetWindowAttribute(hwnd, DwmApi.DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS, out RECT frame, Marshal.SizeOf<RECT>());
        if (!result.Succeeded)
        {
            bounds = default;
            return false;
        }

        bounds = Rectangle.FromLTRB(frame.left, frame.top, frame.right, frame.bottom);
        return true;
    }
    
    public static Rectangle DwmGetWindowFrameBounds(IntPtr hwnd)
    {
        if (!TryGetDwmWindowFrameBounds(hwnd, out var frame))
        {
            return default;
        }

        return frame;
    }
    
    public static bool IsDWMEnabled()
    {
        return IsWindows10OrGreater() && DwmIsCompositionEnabled();
    }

    public static bool SetWindowLong(IntPtr hwnd, Func<User32.SetWindowLongFlags, User32.SetWindowLongFlags> flagsChanger)
    {
        var existingStyle = (User32.SetWindowLongFlags) User32.GetWindowLong(hwnd, User32.WindowLongIndexFlags.GWL_STYLE);
        var newStyle = flagsChanger(existingStyle);
        var newStyleResult = User32.SetWindowLong(hwnd, User32.WindowLongIndexFlags.GWL_STYLE, newStyle);
        return true;
    }

    public static bool SetWindowRect(IntPtr hwnd, Rectangle rect)
    {
        Log.Debug($"[{hwnd.ToHexadecimal()}] Setting window bounds: {rect}");
        Win32ErrorCode error;
        if (!User32.SetWindowPos(hwnd, User32.SpecialWindowHandles.HWND_TOP, rect.X, rect.Y, rect.Width, rect.Height, User32.SetWindowPosFlags.SWP_NOACTIVATE) && (error = Kernel32.GetLastError()) != Win32ErrorCode.NERR_Success)
        {
            Log.Warn($"Failed to SetWindowPos({hwnd.ToHexadecimal()}, {User32.SpecialWindowHandles.HWND_TOPMOST}), error: {error}");
            return false;
        }

        return true;
    }

    public static void HideSystemMenu(IntPtr hwnd)
    {
        Guard.ArgumentIsTrue(hwnd != IntPtr.Zero, "Handle must be non-zero");

        Log.Debug($"[{hwnd.ToHexadecimal()}] Hiding SystemMenu");

        var existingStyle = (User32.SetWindowLongFlags) User32.GetWindowLong(hwnd, User32.WindowLongIndexFlags.GWL_STYLE);
        var newStyle = existingStyle & ~User32.SetWindowLongFlags.WS_SYSMENU;
        if (User32.SetWindowLong(hwnd, User32.WindowLongIndexFlags.GWL_STYLE, newStyle) == 0)
        {
            Log.Warn($"Failed to SetWindowLong to {newStyle} (previously {existingStyle}) of Window by HWND {hwnd.ToHexadecimal()}");
        }
    }

    public static void ShowSystemMenu(IntPtr hwnd)
    {
        Guard.ArgumentIsTrue(hwnd != IntPtr.Zero, "Handle must be non-zero");

        Log.Debug($"[{hwnd.ToHexadecimal()}] Showing SystemMenu");

        var existingStyle = (User32.SetWindowLongFlags) User32.GetWindowLong(hwnd, User32.WindowLongIndexFlags.GWL_STYLE);
        var newStyle = existingStyle | User32.SetWindowLongFlags.WS_SYSMENU;
        if (User32.SetWindowLong(hwnd, User32.WindowLongIndexFlags.GWL_STYLE, newStyle) == 0)
        {
            Log.Warn($"Failed to SetWindowLong to {newStyle} (previously {existingStyle}) of Window by HWND {hwnd.ToHexadecimal()}");
        }
    }

    public static bool SetWindowExTransparent(IntPtr hwnd)
    {
        Guard.ArgumentIsTrue(hwnd != IntPtr.Zero, "Handle must be non-zero");

        Log.Debug($"[{hwnd.ToHexadecimal()}] Reconfiguring window to Transparent");

        var existingStyle = (User32.SetWindowLongFlags) User32.GetWindowLong(hwnd, User32.WindowLongIndexFlags.GWL_EXSTYLE);
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
        Guard.ArgumentIsTrue(hwnd != IntPtr.Zero, "Handle must be non-zero");

        Log.Debug($"[{hwnd.ToHexadecimal()}] Reconfiguring window to Layered");

        var existingStyle = (User32.SetWindowLongFlags) User32.GetWindowLong(hwnd, User32.WindowLongIndexFlags.GWL_EXSTYLE);
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
        Guard.ArgumentIsTrue(hwnd != IntPtr.Zero, "Handle must be non-zero");
        Log.Debug($"[{hwnd.ToHexadecimal()}] Reconfiguring window to NoActivate");
        var existingStyle = (User32.SetWindowLongFlags) User32.GetWindowLong(hwnd, User32.WindowLongIndexFlags.GWL_EXSTYLE);
        var newStyle = existingStyle;
        newStyle |= User32.SetWindowLongFlags.WS_EX_NOACTIVATE;
        if (User32.SetWindowLong(hwnd, User32.WindowLongIndexFlags.GWL_EXSTYLE, newStyle) == 0)
        {
            Log.Warn($"Failed to SetWindowLong to {newStyle} (previously {existingStyle}) of Window by HWND {hwnd.ToHexadecimal()}");
            return false;
        }

        return true;
    }

    public static bool SetWindowExActivate(IntPtr hwnd)
    {
        Guard.ArgumentIsTrue(hwnd != IntPtr.Zero, "Handle must be non-zero");
        Log.Debug($"[{hwnd.ToHexadecimal()}] Reconfiguring window to Activate");
        var existingStyle = (User32.SetWindowLongFlags) User32.GetWindowLong(hwnd, User32.WindowLongIndexFlags.GWL_EXSTYLE);
        var newStyle = existingStyle.RemoveFlag(User32.SetWindowLongFlags.WS_EX_NOACTIVATE);
        if (User32.SetWindowLong(hwnd, User32.WindowLongIndexFlags.GWL_EXSTYLE, newStyle) == 0)
        {
            Log.Warn($"Failed to SetWindowLong to {newStyle} (previously {existingStyle}) of Window by HWND {hwnd.ToHexadecimal()}");
            return false;
        }

        return true;
    }

    public static void ShowTopmost(IntPtr handle)
    {
        Log.Debug($"[{handle.ToHexadecimal()}] Showing window topmost");
        Win32ErrorCode error;
        if (!User32.SetWindowPos(handle,
                User32.SpecialWindowHandles.HWND_TOPMOST,
                0, 0, 0, 0,
                User32.SetWindowPosFlags.SWP_NOMOVE | User32.SetWindowPosFlags.SWP_NOSIZE) && (error = Kernel32.GetLastError()) != Win32ErrorCode.NERR_Success)
        {
            Log.Warn($"Failed to SetWindowPos({handle.ToHexadecimal()}), error: {error}");
        }
    }
    
    public static void ShowInactiveTopmost(IntPtr handle)
    {
        Log.Debug($"[{handle.ToHexadecimal()}] Showing window inactive topmost");
        Win32ErrorCode error;
        if (!User32.ShowWindow(handle, User32.WindowShowStyle.SW_SHOWNOACTIVATE) && (error = Kernel32.GetLastError()) != Win32ErrorCode.NERR_Success)
        {
            Log.Warn($"Failed to ShowWindow({handle.ToHexadecimal()}), error: {error}");
        }

        if (!User32.SetWindowPos(handle,
                User32.SpecialWindowHandles.HWND_TOPMOST,
                0, 0, 0, 0,
                User32.SetWindowPosFlags.SWP_NOMOVE | User32.SetWindowPosFlags.SWP_NOSIZE | User32.SetWindowPosFlags.SWP_NOACTIVATE) && (error = Kernel32.GetLastError()) != Win32ErrorCode.NERR_Success)
        {
            Log.Warn($"Failed to SetWindowPos({handle.ToHexadecimal()}), error: {error}");
        }
    }

    public static void ShowInactiveTopmost(IntPtr handle, Rectangle windowBounds)
    {
        ShowInactiveTopmost(handle);
        Log.Debug($"[{handle.ToHexadecimal()}] Showing window as inactive topmost at {windowBounds}");
        if (!User32.SetWindowPos(handle, User32.SpecialWindowHandles.HWND_TOPMOST, windowBounds.X, windowBounds.Y, windowBounds.Width, windowBounds.Height, User32.SetWindowPosFlags.SWP_NOACTIVATE))
        {
            Log.Warn($"Failed to SetWindowPos({handle.ToHexadecimal()}, {User32.SpecialWindowHandles.HWND_TOPMOST}), error: {Kernel32.GetLastError()}");
        }
    }

    public static bool ShowWindow(IntPtr handle)
    {
        return ShowWindow(handle, User32.WindowShowStyle.SW_SHOWNORMAL);
    }

    public static bool ShowWindow(IntPtr handle, User32.WindowShowStyle showStyle)
    {
        Guard.ArgumentIsTrue(handle != IntPtr.Zero, "Handle must be non-zero");

        Log.Debug($"[{handle.ToHexadecimal()}] Showing window with {showStyle}");
        Win32ErrorCode error;
        if (!User32.ShowWindow(handle, showStyle) && (error = Kernel32.GetLastError()) != Win32ErrorCode.NERR_Success)
        {
            Log.Warn($"Failed to ShowWindow({handle.ToHexadecimal()}), error: {error}");
            return false;
        }

        return true;
    }

    public static bool WindowIsVisible(IntPtr hwnd)
    {
        return User32.IsWindowVisible(hwnd);
    }

    public static bool HideWindow(IntPtr handle)
    {
        Guard.ArgumentIsTrue(handle != IntPtr.Zero, "Handle must be non-zero");

        Log.Debug($"[{handle.ToHexadecimal()}] Hiding window");

        Win32ErrorCode error;
        if (!User32.ShowWindow(handle, User32.WindowShowStyle.SW_HIDE) && (error = Kernel32.GetLastError()) != Win32ErrorCode.NERR_Success)
        {
            Log.Warn($"Failed to HideWindow({handle.ToHexadecimal()}), error: {error}");
            return false;
        }

        return true;
    }

    public static float GetDisplayScaleFactor(IntPtr hwnd)
    {
        try
        {
            return User32.GetDpiForWindow(hwnd) / 96f;
        }
        catch
        {
            // Or fallback to GDI solutions above
            return 1;
        }
    }

    /// <summary>
    /// Resolves the best parent window for Open/Save File, BrowseFolder and other dialogs
    /// Takes into consideration which thread/dispatcher is currently active
    /// </summary>
    /// <returns></returns>
    public static IntPtr ResolveParentForDialogWindow()
    {
        IntPtr result;
        if (System.Windows.Application.Current != null &&
            System.Windows.Application.Current.Dispatcher.CheckAccess())
        {
            if (System.Windows.Application.Current.MainWindow != null)
            {
                // we're on main app dispatcher, try to get main window handle
                var windowHandle = new WindowInteropHelper(System.Windows.Application.Current.MainWindow);
                if (windowHandle.Handle != IntPtr.Zero)
                {
                    return windowHandle.Handle;
                }
            }
        }

        var foregroundWindow = User32.GetForegroundWindow();
        if (foregroundWindow != IntPtr.Zero)
        {
            return foregroundWindow;
        }

        var activeWindow = User32.GetActiveWindow();
        return activeWindow;
    }

    public static IntPtr GetForegroundWindow()
    {
        return User32.GetForegroundWindow();
    }

    public static void ActivateWindow(IntPtr window)
    {
        ActivateWindow(new WindowHandle(window));
    }

    public static void ActivateWindow(IWindowHandle window)
    {
        ActivateWindow(window, TimeSpan.FromMilliseconds(500));
    }

    public static void ActivateWindow(IWindowHandle window, TimeSpan timeout, IFluentLog log)
    {
        if (window == null || window.Handle == IntPtr.Zero)
        {
            log.Debug($"Window is not specified - nothing to activate");
            return;
        }

        log.Debug($"Performing initial activation check");
        if (window.Handle == GetForegroundWindow())
        {
            log.Debug($"Window is already on a foreground, skipping activation");
            return;
        }

        log.Debug($"Requesting window placement");
        var placement = User32.GetWindowPlacement(window.Handle);
        log.Debug($"Window placement: {new {placement.flags, placement.showCmd, placement.ptMaxPosition, placement.ptMinPosition}}");
        if (placement.showCmd == User32.WindowShowStyle.SW_SHOWMINIMIZED)
        {
            log.Debug($"Restoring minimized window {window} to normal");
            ShowWindow(window.Handle, User32.WindowShowStyle.SW_SHOWNORMAL);
            log.Debug($"Restored minimized window {window} to normal");
        }

        log.Debug($"Bringing window to foreground");
        var activationResult = SetForegroundWindow(log, window, WindowActivationMethod);
        log.Debug($"SetForegroundWindow returned {activationResult}");

        var maxActivationTimeout = timeout <= TimeSpan.Zero ? MinWindowActivationTimeout : timeout;
        var sw = ValueStopwatch.StartNew();
        IntPtr foregroundWindow;
        while ((foregroundWindow = GetForegroundWindow()) != window.Handle)
        {
            if (sw.Elapsed > maxActivationTimeout)
            {
                if (foregroundWindow == IntPtr.Zero)
                {
                    if (sw.Elapsed > MaxWindowActivationTimeout)
                    {
                        log.Warn($"Max global activation timeout {MaxWindowActivationTimeout} elapsed, failed to switch to window {window} in {sw.ElapsedMilliseconds:F0}ms");
                        throw new InvalidStateException($"Failed to switch to window {window} in {sw.ElapsedMilliseconds:F0}ms, foreground window is not found, still activating ?");
                    }

                    continue;
                }

                log.Warn($"Activation timeout {maxActivationTimeout} elapsed, failed to switch to window {window} in {sw.ElapsedMilliseconds:F0}ms");
                throw new InvalidStateException($"Failed to switch to window {window} in {sw.ElapsedMilliseconds:F0}ms, foreground window: {UnsafeNative.GetWindowTitle(foregroundWindow)} {foregroundWindow.ToHexadecimal()}");
            }

            TaskExtensions.Sleep(10);
        }
    }

    public static void ActivateWindow(IWindowHandle window, TimeSpan timeout)
    {
        ActivateWindow(window, timeout, Log.WithSuffix(window));
    }

    public static bool SetForegroundWindow(IntPtr hwnd)
    {
        return SetForegroundWindow(new WindowHandle(hwnd));
    }

    public static bool SetForegroundWindow(IWindowHandle hwnd)
    {
        return SetForegroundWindow(Log.WithSuffix(hwnd), hwnd, WindowActivationMethod);
    }

    public static bool SetForegroundWindow(IFluentLog log, IWindowHandle hwnd, UnsafeWindowActivationMethod activationMethod)
    {
        return activationMethod switch
        {
            UnsafeWindowActivationMethod.AttachThreadInput => SetForegroundWindowWithAttachInput(log, hwnd),
            UnsafeWindowActivationMethod.SendInput => SetForegroundWindowWithSendInputHack(log, hwnd),
            var _ => SetForegroundWindowWithSendInputHack(log, hwnd)
        };
    }

    public static bool SetForegroundWindowWithSendInputHack(IFluentLog log, IWindowHandle hwnd)
    {
        log.Debug($"Initiating SetForegroundWindow({hwnd}) via SendInput hack");
        var initialForegroundWindow = GetForegroundWindow();

        if (User32.IsIconic(hwnd.Handle))
        {
            log.Debug("Window is minimized, restoring its state");
            User32.ShowWindow(hwnd.Handle, User32.WindowShowStyle.SW_RESTORE);
        }

        if (hwnd.Handle == initialForegroundWindow)
        {
            log.Debug($"Window is already foreground");
            return true;
        }

        var inputs = new User32.INPUT[]
        {
            new()
            {
                type = User32.InputType.INPUT_MOUSE,
                Inputs = new User32.INPUT.InputUnion
                {
                    mi = new User32.MOUSEINPUT
                    {
                        dx = 0,
                        dy = 0,
                        mouseData = 0,
                        dwFlags = User32.MOUSEEVENTF.MOUSEEVENTF_MOVE,
                        time = 0,
                        dwExtraInfo_IntPtr = IntPtr.Zero
                    }
                }
            }
        };
        var eventsSent = User32.SendInput(inputs.Length, inputs, Marshal.SizeOf<User32.INPUT>());
        if (eventsSent != inputs.Length)
        {
            log.Warn($"Failed to SendInput events, expected {inputs.Length}, got {eventsSent}");
        }

        var maxAttempts = 5;
        var attemptIdx = 0;
        while (!AttemptSetForegroundWindow(log, hwnd))
        {
            if (attemptIdx >= maxAttempts)
            {
                log.Debug($"Failed to SetForegroundWindow miserably after {maxAttempts} attempts");
                return false;
            }
            else
            {
                log.Debug($"Failed to SetForegroundWindow, attempt {attemptIdx + 1}/{maxAttempts}");
            }

            attemptIdx++;
        }

        log.Debug($"SetForegroundWindow succeeded on attempt {attemptIdx + 1}/{maxAttempts}");

        log.Debug("SetForegroundWindow succeeded, bringing window to top...");
        Win32ErrorCode error;
        
        if (!BringWindowToTop(hwnd.Handle) && (error = Kernel32.GetLastError()) != Win32ErrorCode.NERR_Success)
        {
            log.Warn($"Could not SetForegroundWindow.BringWindowToTop, error: {error}");
        }

        return true;
    }

    public static bool SetForegroundWindowWithAttachInput(IFluentLog log, IWindowHandle hwnd)
    {
        log.Debug($"Initiating SetForegroundWindow({hwnd}) with AttachInput");
        var initialForegroundWindow = GetForegroundWindow();

        if (User32.IsIconic(hwnd.Handle))
        {
            log.Debug("Window is minimized, restoring its state");
            User32.ShowWindow(hwnd.Handle, User32.WindowShowStyle.SW_RESTORE);
        }

        if (hwnd.Handle == initialForegroundWindow)
        {
            log.Debug($"Window is already foreground");
            return true;
        }

        if (AttemptSetForegroundWindow(log, hwnd))
        {
            log.Debug($"Activated window without any workarounds");
            return true;
        }

        log.Debug("Initial attempt to SetForegroundWindow has failed");
        var mainThreadId = MainWindowThreadResolver.Instance.GetMainWindowThreadId();
        var foregroundWindowThreadId = initialForegroundWindow != IntPtr.Zero ? GetWindowThreadProcessId(initialForegroundWindow, IntPtr.Zero) : 0;

        using (AttachThreadInput(log, mainThreadId, foregroundWindowThreadId))
        using (AttachThreadInput(log, foregroundWindowThreadId, hwnd.ThreadId))
        {
            var maxAttempts = 5;
            var attemptIdx = 0;
            while (!AttemptSetForegroundWindow(log, hwnd))
            {
                if (attemptIdx >= maxAttempts)
                {
                    log.Debug($"Failed to SetForegroundWindow miserably after {maxAttempts} attempts");
                    return false;
                }
                else
                {
                    log.Debug($"Failed to SetForegroundWindow, attempt {attemptIdx + 1}/{maxAttempts}");
                }

                attemptIdx++;
            }

            log.Debug($"SetForegroundWindow succeeded on attempt {attemptIdx + 1}/{maxAttempts}");
        }

        log.Debug("SetForegroundWindow succeeded after attaching ThreadInput, bringing window to top...");
        Win32ErrorCode error;
        if (!BringWindowToTop(hwnd.Handle) && (error = Kernel32.GetLastError()) != Win32ErrorCode.NERR_Success)
        {
            log.Warn($"Failed to SetForegroundWindow.BringWindowToTop, error: {error}");
        }

        return true;
    }

    public static bool SetFocus(IFluentLog log, IntPtr windowHandle)
    {
        var focused = SetFocus(windowHandle);
        if (focused != windowHandle)
        {
            log.Warn($"Failed to SetFocus, error: {Kernel32.GetLastError()}, focused: {new WindowHandle(focused)}");
            return false;
        }

        return true;
    }

    private static bool AttemptSetForegroundWindow(IFluentLog log, IWindowHandle window)
    {
        log.Debug("Calling SetForegroundWindow");
        var result = User32.SetForegroundWindow(window.Handle); // SetForegroundWindow may lie, result must be double-checked via GetForegroundWindow
        log.Debug($"Call result for SetForegroundWindow is {result}, double-checking...");
        var sw = ValueStopwatch.StartNew();
        IntPtr foregroundWindow;
        while ((foregroundWindow = GetForegroundWindow()) != window.Handle &&
               User32.GetWindow(foregroundWindow, User32.GetWindowCommands.GW_OWNER) != window.Handle)
        {
            if (sw.Elapsed > MinWindowActivationTimeout)
            {
                if (result && foregroundWindow == IntPtr.Zero)
                {
                    log.Debug($"SetForegroundWindow result is OK, but failed to wait for GetForegroundWindow to become {window} in {sw.ElapsedMilliseconds:F0}ms, foreground window is null");
                    break;
                }

                log.Debug($"Failed to SetForegroundWindow {window} in {sw.ElapsedMilliseconds:F0}ms, foreground window: {UnsafeNative.GetWindowTitle(foregroundWindow)} {foregroundWindow.ToHexadecimal()}");
                return false;
            }

            Thread.Sleep(1); //force context switch
        }

        if (result == false)
        {
            log.Debug("Successfully SetForegroundWindow, albeit result of call being false");
        }
        else
        {
            log.Debug("Successfully SetForegroundWindow");
        }

        return true;
    }

    private sealed class MainWindowThreadResolver
    {
        private static readonly Lazy<MainWindowThreadResolver> InstanceSupplier = new();
        private int mainWindowThreadId;

        public static MainWindowThreadResolver Instance => InstanceSupplier.Value;

        public int GetMainWindowThreadId()
        {
            if (mainWindowThreadId != 0)
            {
                return mainWindowThreadId;
            }

            var mainWindowHandle = CurrentProcess.MainWindowHandle;
            if (mainWindowHandle == IntPtr.Zero)
            {
                return 0;
            }

            var threadId = GetWindowThreadProcessId(mainWindowHandle, IntPtr.Zero);
            if (threadId == 0)
            {
                return 0;
            }

            Log.Info($"Main window {mainWindowHandle.ToHexadecimal()} threadId is {threadId}");
            mainWindowThreadId = threadId;
            return mainWindowThreadId;
        }
    }
}