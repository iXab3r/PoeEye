using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using PInvoke;
using PoeShared.Scaffolding;
using PoeShared.Logging;

namespace PoeShared.Native;

public partial class UnsafeNative
{
    /// <summary>
    /// In some cases GetForegroundWindow returns NULL, that means that "future" foreground window is still activating
    /// If system returns NULL we want to give a chance for window to activate. This is max timeout we'll wait for to GetForegroundWindow to return non-null value
    /// </summary>
    private static readonly TimeSpan MaxWindowActivationTimeout = TimeSpan.FromSeconds(10);

    private static readonly TimeSpan MinWindowActivationTimeout = TimeSpan.FromMilliseconds(10);

    [DllImport("dwmapi.dll")]
    private static extern HResult DwmGetWindowAttribute(IntPtr hwnd, DwmApi.DWMWINDOWATTRIBUTE dwAttribute, out RECT pvAttribute, int cbAttribute);

    [DllImport("dwmapi.dll")]
    private static extern HResult DwmGetWindowAttribute(IntPtr hwnd, DwmApi.DWMWINDOWATTRIBUTE dwAttribute, out bool pvAttribute, int cbAttribute);

    public static IntPtr MakeLParam(int loWord, int hiWord)
    {
        return new IntPtr((hiWord << 16) | (loWord & 0xffff));
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

    public static Rectangle GetWindowBoundsWithFrame(IntPtr hwnd)
    {
        //FIXME On Win10 GetWindowRect works not as expected - it includes invisible borders around the frame
        var windowRect = GetWindowRect(hwnd);
        return windowRect;
    }

    public static Rectangle GetClientRectWithinMonitor(IntPtr hwnd)
    {
        var windowRect = GetWindowBoundsWithFrame(hwnd);
        var monitorRect = System.Windows.Forms.Screen.FromHandle(hwnd).Bounds;
        return new Rectangle(windowRect.X - monitorRect.X, windowRect.Y - monitorRect.Y, windowRect.Width, windowRect.Height);
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

    public static Rectangle DwmGetWindowFrameBounds(IntPtr hwnd)
    {
        if (!IsWindows10OrGreater())
        {
            return Rectangle.Empty;
        }

        var result = DwmGetWindowAttribute(hwnd, DwmApi.DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS, out RECT frame, Marshal.SizeOf<RECT>());
        if (!result.Succeeded)
        {
            Log.Warn($"Failed to DwmGetWindowAttribute by HWND {hwnd.ToHexadecimal()}, error: {result}");
            return Rectangle.Empty;
        }

        return new Rectangle(frame.left, frame.top, frame.right - frame.left, frame.bottom - frame.top);
    }

    public static bool SetWindowRect(IntPtr hwnd, Rectangle rect)
    {
        Log.Debug(() => $"[{hwnd.ToHexadecimal()}] Setting window bounds: {rect}");
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

        Log.Debug(() => $"[{hwnd.ToHexadecimal()}] Hiding SystemMenu");

        var existingStyle = (User32.SetWindowLongFlags)User32.GetWindowLong(hwnd, User32.WindowLongIndexFlags.GWL_STYLE);
        var newStyle = existingStyle & ~User32.SetWindowLongFlags.WS_SYSMENU;
        if (User32.SetWindowLong(hwnd, User32.WindowLongIndexFlags.GWL_STYLE, newStyle) == 0)
        {
            Log.Warn($"Failed to SetWindowLong to {newStyle} (previously {existingStyle}) of Window by HWND {hwnd.ToHexadecimal()}");
        }
    }

    public static bool SetWindowExTransparent(IntPtr hwnd)
    {
        Guard.ArgumentIsTrue(hwnd != IntPtr.Zero, "Handle must be non-zero");

        Log.Debug(() => $"[{hwnd.ToHexadecimal()}] Reconfiguring window to Transparent");

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
        Guard.ArgumentIsTrue(hwnd != IntPtr.Zero, "Handle must be non-zero");

        Log.Debug(() => $"[{hwnd.ToHexadecimal()}] Reconfiguring window to Layered");

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
        Guard.ArgumentIsTrue(hwnd != IntPtr.Zero, "Handle must be non-zero");
        Log.Debug(() => $"[{hwnd.ToHexadecimal()}] Reconfiguring window to NoActivate");
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

    public static void ShowInactiveTopmost(IntPtr handle)
    {
        Log.Debug(() => $"[{handle.ToHexadecimal()}] Showing window topmost");
        Win32ErrorCode error;
        if (!User32.ShowWindow(handle, User32.WindowShowStyle.SW_SHOWNOACTIVATE) && (error = Kernel32.GetLastError()) != Win32ErrorCode.NERR_Success)
        {
            Log.Warn($"Failed to ShowWindow({handle.ToHexadecimal()}), error: {error}");
        }
    }

    public static void ShowInactiveTopmost(IntPtr handle, Rectangle windowBounds)
    {
        ShowInactiveTopmost(handle);
        Log.Debug(() => $"[{handle.ToHexadecimal()}] Showing window as inactive topmost at {windowBounds}");
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

        Log.Debug(() => $"[{handle.ToHexadecimal()}] Showing window with {showStyle}");
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

        Log.Debug(() => $"[{handle.ToHexadecimal()}] Hiding window");

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

    public static IntPtr GetForegroundWindow()
    {
        return User32.GetForegroundWindow();
    }

    public static void ActivateWindow(IWindowHandle window)
    {
        ActivateWindow(window, TimeSpan.FromMilliseconds(500));
    }

    public static void ActivateWindow(IWindowHandle window, TimeSpan timeout, IFluentLog log)
    {
        if (window == null || window.Handle == IntPtr.Zero)
        {
            log.Debug(() => $"Window is not specified - nothing to activate");
            return;
        }

        log.Debug(() => $"Performing initial activation check");
        if (window.Handle == GetForegroundWindow())
        {
            log.Debug(() => $"Window is already on a foreground, skipping activation");
            return;
        }

        log.Debug(() => $"Requesting window placement");
        var placement = User32.GetWindowPlacement(window.Handle);
        log.Debug(() => $"Window placement: {new { placement.flags, placement.showCmd, placement.ptMaxPosition, placement.ptMinPosition }}");
        if (placement.showCmd == User32.WindowShowStyle.SW_SHOWMINIMIZED)
        {
            log.Debug(() => $"Restoring minimized window {window} to normal");
            ShowWindow(window.Handle, User32.WindowShowStyle.SW_SHOWNORMAL);
            log.Debug(() => $"Restored minimized window {window} to normal");
        }

        log.Debug(() => $"Bringing window to foreground");
        var activationResult = SetForegroundWindow(window, log);
        log.Debug(() => $"SetForegroundWindow returned {activationResult}");

        var maxActivationTimeout = timeout <= TimeSpan.Zero ? MinWindowActivationTimeout : timeout;
        var sw = Stopwatch.StartNew();
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

            Thread.Sleep(10);
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
        return SetForegroundWindow(hwnd, Log.WithSuffix(hwnd));
    }
    
    public static bool SetForegroundWindow(IWindowHandle hwnd, IFluentLog log)
    {
        log.Debug(() => $"Initiating SetForegroundWindow");
        var initialForegroundWindow = GetForegroundWindow();

        if (User32.IsIconic(hwnd.Handle))
        {
            log.Debug(() => "Window is minimized, restoring its state");
            User32.ShowWindow(hwnd.Handle, User32.WindowShowStyle.SW_RESTORE);
        }

        if (hwnd.Handle == initialForegroundWindow)
        {
            log.Debug(() => $"Window is already foreground");
            return true;
        }

        if (AttemptSetForegroundWindow(hwnd))
        {
            log.Debug(() => $"Activated window without any workarounds");
            return true;
        }

        log.Debug(() => "Initial attempt to SetForegroundWindow has failed");
        var mainThreadId = MainWindowThreadResolver.Instance.GetMainWindowThreadId();
        var foregroundWindowThreadId = initialForegroundWindow != IntPtr.Zero ? GetWindowThreadProcessId(initialForegroundWindow, IntPtr.Zero) : 0;

        using (AttachThreadInput(log, mainThreadId, foregroundWindowThreadId))
        using (AttachThreadInput(log, foregroundWindowThreadId, hwnd.ThreadId))
        {
            var maxAttempts = 5;
            var attemptIdx = 0;
            while (!AttemptSetForegroundWindow(hwnd))
            {
                if (attemptIdx >= maxAttempts)
                {
                    log.Debug(() => $"Failed to SetForegroundWindow miserably after {maxAttempts} attempts");
                    return false;
                }
                else
                {
                    log.Debug(() => $"Failed to SetForegroundWindow, attempt {attemptIdx + 1}/{maxAttempts}");
                }

                attemptIdx++;
            }

            log.Debug(() => $"SetForegroundWindow succeeded on attempt {attemptIdx + 1}/{maxAttempts}");
        }

        log.Debug(() => "SetForegroundWindow succeeded after attaching ThreadInput, bringing window to top...");
        Win32ErrorCode error;
        if (!BringWindowToTop(hwnd.Handle) && (error = Kernel32.GetLastError()) != Win32ErrorCode.NERR_Success)
        {
            log.Warn($"Failed to SetForegroundWindow.BringWindowToTop, error: {error}");
        }

        return true;
    }

    private static bool AttemptSetForegroundWindow(IWindowHandle window)
    {
        var log = Log.WithSuffix(window);
        log.Debug(() => "Calling SetForegroundWindow");
        var result = User32.SetForegroundWindow(window.Handle); // SetForegroundWindow may lie, result must be double-checked via GetForegroundWindow
        log.Debug(() => $"Call result for SetForegroundWindow is {result}, double-checking...");
        var sw = Stopwatch.StartNew();
        IntPtr foregroundWindow;
        while ((foregroundWindow = GetForegroundWindow()) != window.Handle && User32.GetWindow(foregroundWindow, User32.GetWindowCommands.GW_OWNER) != window.Handle)
        {
            if (sw.Elapsed > MinWindowActivationTimeout)
            {
                log.Warn($"Failed to SetForegroundWindow {window} in {sw.ElapsedMilliseconds:F0}ms, foreground window: {UnsafeNative.GetWindowTitle(foregroundWindow)} {foregroundWindow.ToHexadecimal()}");
                return false;
            }

            Thread.Sleep(1);
        }

        if (result == false)
        {
            log.Warn(() => "Successfully SetForegroundWindow, albeit result of call being false");
        }
        else
        {
            log.Debug(() => "Successfully SetForegroundWindow");
        }

        return true;
    }

    private sealed class MainWindowThreadResolver
    {
        private static readonly Lazy<MainWindowThreadResolver> InstanceSupplier = new Lazy<MainWindowThreadResolver>();
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