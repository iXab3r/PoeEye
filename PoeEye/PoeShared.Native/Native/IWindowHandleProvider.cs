using System;

namespace PoeShared.Native;

public interface IWindowHandleProvider
{
    IWindowHandle GetByWindowHandle(IntPtr hwnd);
}