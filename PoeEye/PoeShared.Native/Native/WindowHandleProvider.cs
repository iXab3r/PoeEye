using System;

namespace PoeShared.Native;

internal sealed class WindowHandleProvider : IWindowHandleProvider
{
    public IWindowHandle GetByWindowHandle(IntPtr hwnd)
    {
        return new WindowHandle(hwnd);
    }
}