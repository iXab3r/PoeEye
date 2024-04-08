using PInvoke;
using PoeShared.Scaffolding;

namespace PoeShared.Native;

internal sealed class OverlayWindowMatcher : IWindowTrackerMatcher
{
    public bool IsMatch(IWindowHandle window)
    {
        var isOwnWindow = window.IsOwnWindow();
        if (!isOwnWindow)
        {
            return false;
        }

        //previously, overlays were detected this way
        //var isOverlay = window.WindowStylesEx.HasFlag(User32.WindowStylesEx.WS_EX_LAYERED | User32.WindowStylesEx.WS_EX_TOOLWINDOW | User32.WindowStylesEx.WS_EX_TOPMOST);
        var isLayered = window.WindowStylesEx.HasFlag(User32.WindowStylesEx.WS_EX_LAYERED);
        return isLayered;
    }
}