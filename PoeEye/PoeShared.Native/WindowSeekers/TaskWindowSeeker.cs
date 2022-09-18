using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using PInvoke;

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

    /// <summary>
    /// Delegate that is called by EnumWindow
    /// </summary>
    /// <param name="handle"></param>
    /// <param name="addWindowHandler"></param>
    /// <returns>True to skip(duh) the window</returns>
    private bool InspectWindow(IWindowHandle handle, Action<IWindowHandle> addWindowHandler)
    {
        //Some parts are taken from http://www.thescarms.com/VBasic/alttab.aspx
        if (string.IsNullOrEmpty(handle.Title))
        {
            //skip - empty titles will not allow for evaluation
            return true;
        }

        //Accept windows that
        // - have no owner and are not Tool windows OR
        // - have an owner and are App windows
        if (handle.Parent != null)
        {
            // skip - this is child window
            return true;
        }

        addWindowHandler(handle);
        return true;
    }
        
}