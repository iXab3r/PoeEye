using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PoeShared.UI.E2E;

internal static class NativeWindowInterop
{
    private const uint MouseEventLeftDown = 0x0002;
    private const uint MouseEventLeftUp = 0x0004;
    private const uint WmNcHitTest = 0x0084;
    private const uint WmClose = 0x0010;
    private const int GwlStyle = -16;
    private const int HtBottom = 15;
    private const int HtRight = 11;
    private const int HtBottomRight = 17;

    public static NativeRect GetWindowRect(IntPtr hwnd)
    {
        if (!GetWindowRect(hwnd, out var rect))
        {
            throw new InvalidOperationException($"GetWindowRect failed for 0x{hwnd.ToInt64():X}.");
        }

        return rect;
    }

    public static bool HasResizeHitTargets(IntPtr hwnd)
    {
        var rect = GetWindowRect(hwnd);
        return Enumerable.Range(1, 12).Any(offset =>
            HitTest(hwnd, rect.Right - offset, rect.Bottom - offset) == HtBottomRight &&
            HitTest(hwnd, rect.Right - offset, rect.Top + ((rect.Bottom - rect.Top) / 2)) == HtRight &&
            HitTest(hwnd, rect.Left + ((rect.Right - rect.Left) / 2), rect.Bottom - offset) == HtBottom);
    }

    public static async Task<bool> TryResizeBottomRightAsync(IntPtr hwnd, int deltaX, int deltaY, CancellationToken cancellationToken = default)
    {
        foreach (var xOffset in new[] { -12, -8, -4, 0, 4, 8, 12, 16, 20, 24 })
        {
            foreach (var yOffset in new[] { -12, -8, -4, 0, 4, 8, 12, 16, 20, 24 })
            {
                if (await TryResizeBottomRightAsync(hwnd, deltaX, deltaY, xOffset, yOffset, cancellationToken))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static async Task DragMouseAsync(int startX, int startY, int endX, int endY, int steps = 12, CancellationToken cancellationToken = default)
    {
        steps = Math.Max(1, steps);

        SetCursorPos(startX, startY);
        await Task.Delay(120, cancellationToken);

        SendMouseInput(MouseEventLeftDown);
        await Task.Delay(120, cancellationToken);

        for (var step = 1; step <= steps; step++)
        {
            var x = startX + (int)Math.Round((endX - startX) * (step / (double)steps));
            var y = startY + (int)Math.Round((endY - startY) * (step / (double)steps));
            SetCursorPos(x, y);
            await Task.Delay(25, cancellationToken);
        }

        await Task.Delay(150, cancellationToken);
        SendMouseInput(MouseEventLeftUp);
        await Task.Delay(300, cancellationToken);
    }

    public static async Task ClickMouseAsync(int x, int y, CancellationToken cancellationToken = default)
    {
        SetCursorPos(x, y);
        await Task.Delay(120, cancellationToken);
        SendMouseInput(MouseEventLeftDown);
        await Task.Delay(80, cancellationToken);
        SendMouseInput(MouseEventLeftUp);
        await Task.Delay(250, cancellationToken);
    }

    private static int HitTest(IntPtr hwnd, int x, int y)
    {
        return unchecked((short)SendMessage(hwnd, WmNcHitTest, IntPtr.Zero, MakeLParam(x, y)).ToInt64());
    }

    public static IReadOnlyList<TopLevelWindowInfo> GetVisibleTopLevelWindows()
    {
        var result = new List<TopLevelWindowInfo>();
        var handle = GCHandle.Alloc(result);
        try
        {
            EnumWindows(
                static (hwnd, lParam) =>
                {
                    var windows = (List<TopLevelWindowInfo>)GCHandle.FromIntPtr(lParam).Target!;
                    if (!IsWindowVisible(hwnd))
                    {
                        return true;
                    }

                    var length = GetWindowTextLength(hwnd);
                    var titleBuilder = new System.Text.StringBuilder(Math.Max(length + 1, 1));
                    GetWindowText(hwnd, titleBuilder, titleBuilder.Capacity);
                    var title = titleBuilder.ToString();
                    if (string.IsNullOrWhiteSpace(title))
                    {
                        return true;
                    }

                    GetWindowThreadProcessId(hwnd, out var processId);
                    var processName = string.Empty;
                    if (processId != 0)
                    {
                        try
                        {
                            processName = Process.GetProcessById((int)processId).ProcessName;
                        }
                        catch
                        {
                            processName = string.Empty;
                        }
                    }

                    windows.Add(new TopLevelWindowInfo(hwnd, title, processId, processName));
                    return true;
                },
                GCHandle.ToIntPtr(handle));
        }
        finally
        {
            handle.Free();
        }

        return result;
    }

    public static void CloseWindow(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        PostMessage(hwnd, WmClose, IntPtr.Zero, IntPtr.Zero);
    }

    public static void ActivateWindow(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        SetForegroundWindow(hwnd);
    }

    public static nint GetWindowStyle(IntPtr hwnd)
    {
        return GetWindowLongPtr(hwnd, GwlStyle);
    }

    private static IntPtr MakeLParam(int x, int y)
        => (IntPtr)(((y & 0xFFFF) << 16) | (x & 0xFFFF));

    private static async Task<bool> TryResizeBottomRightAsync(IntPtr hwnd, int deltaX, int deltaY, int xOffset, int yOffset, CancellationToken cancellationToken)
    {
        var before = GetWindowRect(hwnd);
        var startX = before.Right - xOffset;
        var startY = before.Bottom - yOffset;

        SetForegroundWindow(hwnd);
        SetCursorPos(startX, startY);
        await Task.Delay(150, cancellationToken);

        SendMouseInput(MouseEventLeftDown);
        await Task.Delay(120, cancellationToken);

        SetCursorPos(startX + Math.Max(4, deltaX / 2), startY + Math.Max(4, deltaY / 2));
        await Task.Delay(120, cancellationToken);

        SetCursorPos(startX + deltaX, startY + deltaY);
        await Task.Delay(320, cancellationToken);
        SendMouseInput(MouseEventLeftUp);
        await Task.Delay(400, cancellationToken);

        var after = GetWindowRect(hwnd);
        return after.Width > before.Width && after.Height > before.Height;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hWnd, out NativeRect lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern nint GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    private static void SendMouseInput(uint flags)
    {
        var inputs = new[]
        {
            new INPUT
            {
                type = 0,
                U = new InputUnion
                {
                    mi = new MOUSEINPUT
                    {
                        dx = 0,
                        dy = 0,
                        mouseData = 0,
                        dwFlags = flags,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            }
        };

        var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        if (sent != inputs.Length)
        {
            throw new InvalidOperationException($"SendInput failed, expected {inputs.Length}, got {sent}.");
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly record struct NativeRect(int Left, int Top, int Right, int Bottom)
    {
        public int Width => Right - Left;

        public int Height => Bottom - Top;
    }

    internal readonly record struct TopLevelWindowInfo(IntPtr Handle, string Title, uint ProcessId, string ProcessName);

    private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
}
