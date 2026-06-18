using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Interop;
using PInvoke;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace PoeShared.Blazor.Wpf;

internal static class UnsafeNative
{
    private static readonly IFluentLog Log = typeof(UnsafeNative).PrepareLogger();

    private const int DwmWindowCornerPreferenceAttribute = 33;
    private const uint TpmRightButton = 0x0002;
    private const uint TpmReturnCommand = 0x0100;

    private enum DwmWindowCornerPreference
    {
        Default = 0,
        DoNotRound = 1,
        Round = 2,
        RoundSmall = 3
    }

    [DllImport("dwmapi.dll", PreserveSig = false)]
    private static extern bool DwmIsCompositionEnabled();

    [DllImport("dwmapi.dll")]
    private static extern HResult DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out int pvAttribute, int cbAttribute);

    [DllImport("dwmapi.dll")]
    private static extern HResult DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetSystemMenu(IntPtr hwnd, bool revert);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int TrackPopupMenuEx(IntPtr menu, uint flags, int x, int y, IntPtr hwnd, IntPtr trackPopupMenuParams);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DrawMenuBar(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool BringWindowToTop(IntPtr hwnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int maxCount);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool EnableWindow(IntPtr hwnd, bool isEnabled);

    [DllImport("user32.dll")]
    private static extern bool IsZoomed(IntPtr hwnd);

    public static Point GetCursorPosition()
    {
        return User32.GetCursorPos();
    }

    public static bool IsWindows10OrGreater(int build = -1)
    {
        var version = Environment.OSVersion.Version;
        return version.Major >= 10 && version.Build >= build;
    }

    public static IntPtr GetForegroundWindow()
    {
        return User32.GetForegroundWindow();
    }

    public static IntPtr ResolveParentForDialogWindow()
    {
        if (System.Windows.Application.Current != null &&
            System.Windows.Application.Current.Dispatcher.CheckAccess() &&
            System.Windows.Application.Current.MainWindow != null)
        {
            var windowHandle = new WindowInteropHelper(System.Windows.Application.Current.MainWindow);
            if (windowHandle.Handle != IntPtr.Zero)
            {
                return windowHandle.Handle;
            }
        }

        var foregroundWindow = User32.GetForegroundWindow();
        if (foregroundWindow != IntPtr.Zero)
        {
            return foregroundWindow;
        }

        return User32.GetActiveWindow();
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

    public static void ConfigureCustomWindowFrame(IntPtr hwnd, bool isResizable)
    {
        Guard.ArgumentIsTrue(hwnd != IntPtr.Zero, "Handle must be non-zero");

        Log.Debug($"[{hwnd.ToHexadecimal()}] Configuring custom window frame: resizable={isResizable}");

        var existingStyle = (User32.SetWindowLongFlags) User32.GetWindowLong(hwnd, User32.WindowLongIndexFlags.GWL_STYLE);
        var newStyle = isResizable
            ? existingStyle | User32.SetWindowLongFlags.WS_THICKFRAME
            : existingStyle & ~User32.SetWindowLongFlags.WS_THICKFRAME;

        if (newStyle != existingStyle && User32.SetWindowLong(hwnd, User32.WindowLongIndexFlags.GWL_STYLE, newStyle) == 0)
        {
            Log.Warn($"Failed to SetWindowLong to {newStyle} (previously {existingStyle}) of Window by HWND {hwnd.ToHexadecimal()}");
        }

        RefreshNonClientFrame(hwnd);
    }

    public static void TryEnableRoundedCorners(IntPtr hwnd)
    {
        Guard.ArgumentIsTrue(hwnd != IntPtr.Zero, "Handle must be non-zero");

        if (!IsDwmEnabled() || !IsWindows10OrGreater(22000))
        {
            return;
        }

        var preference = (int) DwmWindowCornerPreference.Round;
        var result = DwmSetWindowAttribute(hwnd, DwmWindowCornerPreferenceAttribute, ref preference, sizeof(int));
        if (!result.Succeeded)
        {
            Log.Warn($"Failed to enable rounded corners for Window by HWND {hwnd.ToHexadecimal()}, error: {result}");
        }
    }

    public static double GetApproximateWindowCornerRadius(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero || !IsDwmEnabled() || !IsWindows10OrGreater(22000) || IsZoomed(hwnd))
        {
            return 0;
        }

        return GetWindowCornerPreference(hwnd) switch
        {
            DwmWindowCornerPreference.DoNotRound => 0,
            DwmWindowCornerPreference.RoundSmall => 4,
            DwmWindowCornerPreference.Default => 8,
            DwmWindowCornerPreference.Round => 8,
            _ => 8
        };
    }

    public static bool SetWindowRect(IntPtr hwnd, Rectangle rect)
    {
        Log.Debug($"[{hwnd.ToHexadecimal()}] Setting window rect: {rect}");
        if (!User32.SetWindowPos(
                hwnd,
                IntPtr.Zero,
                rect.X,
                rect.Y,
                rect.Width,
                rect.Height,
                User32.SetWindowPosFlags.SWP_NOACTIVATE | User32.SetWindowPosFlags.SWP_NOZORDER) &&
            Kernel32.GetLastError() is { } error &&
            error != Win32ErrorCode.NERR_Success)
        {
            Log.Warn($"Failed to SetWindowPos({hwnd.ToHexadecimal()}, {rect}), error: {error}");
            return false;
        }

        return true;
    }

    public static bool SetWindowPos(IntPtr hwnd, Point location)
    {
        Log.Debug($"[{hwnd.ToHexadecimal()}] Setting window location: {location}");
        if (!User32.SetWindowPos(
                hwnd,
                IntPtr.Zero,
                location.X,
                location.Y,
                0,
                0,
                User32.SetWindowPosFlags.SWP_NOACTIVATE | User32.SetWindowPosFlags.SWP_NOZORDER | User32.SetWindowPosFlags.SWP_NOSIZE) &&
            Kernel32.GetLastError() is { } error &&
            error != Win32ErrorCode.NERR_Success)
        {
            Log.Warn($"Failed to SetWindowPos({hwnd.ToHexadecimal()}, {location}), error: {error}");
            return false;
        }

        return true;
    }

    public static bool SetWindowSize(IntPtr hwnd, Size size)
    {
        Log.Debug($"[{hwnd.ToHexadecimal()}] Setting window size: {size}");
        if (!User32.SetWindowPos(
                hwnd,
                IntPtr.Zero,
                0,
                0,
                size.Width,
                size.Height,
                User32.SetWindowPosFlags.SWP_NOACTIVATE | User32.SetWindowPosFlags.SWP_NOZORDER | User32.SetWindowPosFlags.SWP_NOMOVE) &&
            Kernel32.GetLastError() is { } error &&
            error != Win32ErrorCode.NERR_Success)
        {
            Log.Warn($"Failed to SetWindowPos({hwnd.ToHexadecimal()}, {size}), error: {error}");
            return false;
        }

        return true;
    }

    public static bool ShowSystemMenuAt(IntPtr hwnd, Point screenLocation)
    {
        Guard.ArgumentIsTrue(hwnd != IntPtr.Zero, "Handle must be non-zero");

        var systemMenu = GetSystemMenu(hwnd, revert: false);
        if (systemMenu == IntPtr.Zero)
        {
            var error = Kernel32.GetLastError();
            Log.Warn($"Failed to get system menu for {hwnd.ToHexadecimal()}, error: {error}");
            return false;
        }

        Log.Debug($"[{hwnd.ToHexadecimal()}] Showing SystemMenu at {screenLocation}");
        SetForegroundWindow(hwnd);
        var command = TrackPopupMenuEx(systemMenu, TpmRightButton | TpmReturnCommand, screenLocation.X, screenLocation.Y, hwnd, IntPtr.Zero);
        if (command == 0)
        {
            Log.Debug($"SystemMenu for {hwnd.ToHexadecimal()} was cancelled or no command was selected");
            return false;
        }

        var commandCursorPosition = GetCursorPosition();
        Log.Debug($"[{hwnd.ToHexadecimal()}] Sending SystemMenu command 0x{command:X} at {commandCursorPosition}");
        User32.SendMessage(
            hwnd,
            (User32.WindowMessage) 0x0112,
            (IntPtr) command,
            MakeLParam(commandCursorPosition.X, commandCursorPosition.Y));
        return true;
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
        var newStyle = existingStyle | User32.SetWindowLongFlags.WS_EX_NOACTIVATE;
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
        var newStyle = existingStyle & ~User32.SetWindowLongFlags.WS_EX_NOACTIVATE;
        if (User32.SetWindowLong(hwnd, User32.WindowLongIndexFlags.GWL_EXSTYLE, newStyle) == 0)
        {
            Log.Warn($"Failed to SetWindowLong to {newStyle} (previously {existingStyle}) of Window by HWND {hwnd.ToHexadecimal()}");
            return false;
        }

        return true;
    }

    public static bool SetForegroundWindow(IntPtr hwnd)
    {
        Guard.ArgumentIsTrue(hwnd != IntPtr.Zero, "Handle must be non-zero");

        Log.Debug($"[{hwnd.ToHexadecimal()}] Initiating SetForegroundWindow via SendInput hack");
        var initialForegroundWindow = GetForegroundWindow();

        if (User32.IsIconic(hwnd))
        {
            Log.Debug($"[{hwnd.ToHexadecimal()}] Window is minimized, restoring its state");
            User32.ShowWindow(hwnd, User32.WindowShowStyle.SW_RESTORE);
        }

        if (hwnd == initialForegroundWindow)
        {
            Log.Debug($"[{hwnd.ToHexadecimal()}] Window is already foreground");
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
            Log.Warn($"[{hwnd.ToHexadecimal()}] Failed to SendInput events, expected {inputs.Length}, got {eventsSent}");
        }

        const int maxAttempts = 5;
        var attemptIdx = 0;
        while (!AttemptSetForegroundWindow(hwnd))
        {
            if (attemptIdx >= maxAttempts)
            {
                Log.Debug($"[{hwnd.ToHexadecimal()}] Failed to SetForegroundWindow after {maxAttempts} attempts");
                return false;
            }

            Log.Debug($"[{hwnd.ToHexadecimal()}] Failed to SetForegroundWindow, attempt {attemptIdx + 1}/{maxAttempts}");
            attemptIdx++;
        }

        Log.Debug($"[{hwnd.ToHexadecimal()}] SetForegroundWindow succeeded on attempt {attemptIdx + 1}/{maxAttempts}");
        if (!BringWindowToTop(hwnd) &&
            Kernel32.GetLastError() is { } error &&
            error != Win32ErrorCode.NERR_Success)
        {
            Log.Warn($"[{hwnd.ToHexadecimal()}] Could not SetForegroundWindow.BringWindowToTop, error: {error}");
        }

        return true;
    }

    private static bool IsDwmEnabled()
    {
        return IsWindows10OrGreater() && DwmIsCompositionEnabled();
    }

    private static DwmWindowCornerPreference GetWindowCornerPreference(IntPtr hwnd)
    {
        var result = DwmGetWindowAttribute(hwnd, DwmWindowCornerPreferenceAttribute, out var preference, sizeof(int));
        if (!result.Succeeded || !Enum.IsDefined(typeof(DwmWindowCornerPreference), preference))
        {
            return DwmWindowCornerPreference.Default;
        }

        return (DwmWindowCornerPreference) preference;
    }

    private static bool AttemptSetForegroundWindow(IntPtr hwnd)
    {
        Log.Debug($"[{hwnd.ToHexadecimal()}] Calling SetForegroundWindow");
        var result = User32.SetForegroundWindow(hwnd);
        Log.Debug($"[{hwnd.ToHexadecimal()}] Call result for SetForegroundWindow is {result}, double-checking...");

        var sw = ValueStopwatch.StartNew();
        IntPtr foregroundWindow;
        while ((foregroundWindow = GetForegroundWindow()) != hwnd &&
               User32.GetWindow(foregroundWindow, User32.GetWindowCommands.GW_OWNER) != hwnd)
        {
            if (sw.Elapsed > TimeSpan.FromMilliseconds(10))
            {
                if (result && foregroundWindow == IntPtr.Zero)
                {
                    Log.Debug($"[{hwnd.ToHexadecimal()}] SetForegroundWindow result is OK, but foreground window is still null after {sw.ElapsedMilliseconds:F0}ms");
                    break;
                }

                Log.Debug($"[{hwnd.ToHexadecimal()}] Failed to SetForegroundWindow in {sw.ElapsedMilliseconds:F0}ms, foreground window: {GetWindowTitle(foregroundWindow)} {foregroundWindow.ToHexadecimal()}");
                return false;
            }

            Thread.Sleep(1);
        }

        Log.Debug(result
            ? $"[{hwnd.ToHexadecimal()}] Successfully SetForegroundWindow"
            : $"[{hwnd.ToHexadecimal()}] Successfully SetForegroundWindow, albeit result of call being false");

        return true;
    }

    private static string GetWindowTitle(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
        {
            return null;
        }

        const int maxChars = 256;
        var buffer = new StringBuilder(maxChars);
        return GetWindowText(hwnd, buffer, maxChars) > 0 ? buffer.ToString() : null;
    }

    private static void RefreshNonClientFrame(IntPtr hwnd)
    {
        const User32.SetWindowPosFlags frameChanged = (User32.SetWindowPosFlags) 0x0020;

        if (!User32.SetWindowPos(
                hwnd,
                IntPtr.Zero,
                0,
                0,
                0,
                0,
                User32.SetWindowPosFlags.SWP_NOMOVE |
                User32.SetWindowPosFlags.SWP_NOSIZE |
                User32.SetWindowPosFlags.SWP_NOACTIVATE |
                User32.SetWindowPosFlags.SWP_NOZORDER |
                frameChanged) &&
            Kernel32.GetLastError() is { } error &&
            error != Win32ErrorCode.NERR_Success)
        {
            Log.Warn($"Failed to refresh non-client frame for Window by HWND {hwnd.ToHexadecimal()}, error: {error}");
        }

        if (!DrawMenuBar(hwnd) &&
            Kernel32.GetLastError() is { } drawError &&
            drawError != Win32ErrorCode.NERR_Success)
        {
            Log.Warn($"Failed to redraw menu bar for Window by HWND {hwnd.ToHexadecimal()}, error: {drawError}");
        }
    }

    private static IntPtr MakeLParam(int loWord, int hiWord)
    {
        return new IntPtr((hiWord << 16) | (loWord & 0xffff));
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WINDOWPOS
    {
        public readonly IntPtr hwnd;
        public readonly IntPtr hwndInsertAfter;
        public int x;
        public int y;
        public int cx;
        public int cy;
        public readonly User32.SetWindowPosFlags flags;
    }
}
