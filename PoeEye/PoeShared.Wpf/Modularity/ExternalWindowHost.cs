using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using Microsoft.VisualBasic.Logging;
using PInvoke;
using PoeShared.Native;

namespace PoeShared.Modularity;

public class ExternalWindowHost : HwndHost
{
    private readonly IntPtr windowHandle;

    public ExternalWindowHost(IntPtr windowHandle)
    {
        this.windowHandle = windowHandle;
    }

    protected override HandleRef BuildWindowCore(HandleRef hwndParent)
    {
        var result = SetParent(windowHandle, hwndParent.Handle);
        
        var existingStyle = (User32.SetWindowLongFlags)User32.GetWindowLong(windowHandle, User32.WindowLongIndexFlags.GWL_STYLE);
        var newStyle = existingStyle;
        newStyle |= User32.SetWindowLongFlags.WS_CHILD;
        //newStyle &= ~User32.SetWindowLongFlags.WS_BORDER;
        newStyle &= ~User32.SetWindowLongFlags.WS_SYSMENU;
        var newStyleResult = User32.SetWindowLong(windowHandle, User32.WindowLongIndexFlags.GWL_STYLE, newStyle);
        return new HandleRef(this, windowHandle);
    }

    protected override void DestroyWindowCore(HandleRef hwnd)
    {
        // Clean up child window here
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
}