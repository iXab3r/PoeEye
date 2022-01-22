using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using PInvoke;
using PoeShared.Native;

namespace PoeShared.WindowSeekers;

/// <summary>
///     Window seeker that attempts to mimic ALT+TAB behavior in filtering windows to show.
/// </summary>
public sealed class TaskWindowSeeker : BaseWindowSeeker
{
    private readonly IWindowHandleProvider windowHandleProvider;
    public override IReadOnlyCollection<IWindowHandle> Windows { get; protected set; } = new List<IWindowHandle>();

    public TaskWindowSeeker(IWindowHandleProvider windowHandleProvider)
    {
        this.windowHandleProvider = windowHandleProvider;
    }

    public override void Refresh()
    {
        var windowsSnapshot = new List<IWindowHandle>();
        User32.EnumWindows((hwnd, lParam) => RefreshCallback(
                hwnd,
                handle =>
                {
                    handle.ZOrder = windowsSnapshot.Count;
                    windowsSnapshot.Add(handle);
                }),
            IntPtr.Zero);
        Windows = new ReadOnlyCollection<IWindowHandle>(windowsSnapshot);
    }

    private bool RefreshCallback(IntPtr hwnd, Action<IWindowHandle> addHandler)
    {
        if (BlacklistedWindows.Contains(hwnd))
        {
            return true;
        }

        if (SkipNotVisibleWindows && !User32.IsWindowVisible(hwnd))
        {
            return true;
        }

        var handle = windowHandleProvider.GetByWindowHandle(hwnd);
        return InspectWindow(handle, addHandler);
    }

    private bool InspectWindow(IWindowHandle handle, Action<IWindowHandle> addHandler)
    {
        //Code taken from: http://www.thescarms.com/VBasic/alttab.aspx

        //Reject empty titles
        if (string.IsNullOrEmpty(handle.Title))
        {
            return true;
        }

        //Accept windows that
        // - are visible
        // - do not have a parent
        // - have no owner and are not Tool windows OR
        // - have an owner and are App windows
        if ((long) UnsafeNative.GetParent(handle.Handle) != 0)
        {
            return true;
        }

        var hasOwner = (long) User32.GetWindow(handle.Handle, User32.GetWindowCommands.GW_OWNER) != 0;
        var exStyle = (UnsafeNative.WindowExStyles) UnsafeNative.GetWindowLong(handle.Handle, UnsafeNative.WindowLong.ExStyle);

        if ((exStyle & UnsafeNative.WindowExStyles.ToolWindow) == 0 && !hasOwner || //unowned non-tool window
            (exStyle & UnsafeNative.WindowExStyles.AppWindow) == UnsafeNative.WindowExStyles.AppWindow && hasOwner)
        {
            addHandler(handle);
        }

        return true;
    }
        
}