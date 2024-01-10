using System.Drawing;
using PoeShared.Native;

namespace PoeShared.Scaffolding;

public static class WindowHandleExtensions
{
    public static WinPoint ToScreen(this IWindowHandle windowHandle, Point windowPoint)
    {
        var windowBounds = windowHandle.DwmFrameBounds;
        if (!windowBounds.IsNotEmptyArea())
        {
            throw new InvalidStateException($"Something went wrong - target window {windowHandle} is not found or does not have valid window bounds");
        }

        return new WinPoint(windowPoint.X + windowBounds.Left, windowPoint.Y + windowBounds.Top);
    }
    
    public static WinPoint FromScreen(this IWindowHandle windowHandle, Point screenPoint)
    {
        var windowBounds = windowHandle.DwmFrameBounds;
        if (!windowBounds.IsNotEmptyArea())
        {
            throw new InvalidStateException($"Something went wrong - target window {windowHandle} is not found or does not have valid window bounds");
        }

        return new WinPoint(screenPoint.X - windowBounds.Left, screenPoint.Y - windowBounds.Top);
    }
}