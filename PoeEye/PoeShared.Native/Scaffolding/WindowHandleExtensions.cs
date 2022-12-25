﻿using System;
using System.Drawing;
using PInvoke;

namespace PoeShared.Scaffolding;

public static class WindowHandleExtensions
{
    /// <summary>
    ///   Checks Visibility, WS_CHILD, WS_EX_TOOLWINDOW and other properties to make sure that this window could be interacted with
    /// </summary>
    /// <returns></returns>
    public static bool IsVisibleAndValid(this IWindowHandle windowHandle, bool excludeMinimized = false)
    {
        if (windowHandle.Handle == IntPtr.Zero)
        {
            return false;
        }

        if (windowHandle.Handle == User32.GetShellWindow())
        {
            return false;
        }

        if (excludeMinimized)
        {
            if (!windowHandle.IsVisible)
            {
                return false;
            }
            
            if (windowHandle.IsIconic)
            {
                return false;
            }
            
            if (windowHandle.ClientBounds.Width <= 0 || windowHandle.ClientBounds.Height <= 0)
            {
                return false;
            }
        }
            
        if (User32.GetAncestor(windowHandle.Handle, User32.GetAncestorFlags.GA_ROOT) != windowHandle.Handle)
        {
            return false;
        }
            
        if (windowHandle.WindowStylesEx.HasFlag(User32.WindowStylesEx.WS_EX_TOOLWINDOW))
        {
            return false;
        }

        if (windowHandle.WindowStyle.HasFlag(User32.WindowStyles.WS_CHILD) || windowHandle.WindowStyle.HasFlag(User32.WindowStyles.WS_DISABLED))
        {
            return false;
        }
            
        if (UnsafeNative.DwmGetWindowAttribute(windowHandle.Handle, DwmApi.DWMWINDOWATTRIBUTE.DWMWA_CLOAKED))
        {
            return false;
        }

        return true;
    }
}