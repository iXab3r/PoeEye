using System;
using PInvoke;

namespace PoeShared.Native;

internal sealed class WindowHandleProvider : IWindowHandleProvider
{
    public IWindowHandle GetByWindowHandle(IntPtr hwnd)
    {
        return new WindowHandle(hwnd);
    }

    public IMonitorHandle GetByMonitorHandle(IntPtr hMonitor)
    {
        return new MonitorHandle(hMonitor);
    }

    public IMonitorHandle GetMonitorByWindowHandle(IntPtr hwnd)
    {
        var hMonitor = User32.MonitorFromWindow(hwnd, User32.MonitorOptions.MONITOR_DEFAULTTONEAREST);
        return GetByMonitorHandle(hMonitor);
    }
}