using System;

namespace PoeShared.Native;

public interface IWindowHandleProvider
{
    IWindowHandle GetByWindowHandle(IntPtr hwnd);

    IMonitorHandle GetByMonitorHandle(IntPtr hMonitor);
    
    IMonitorHandle GetMonitorByWindowHandle(IntPtr hwnd);
}