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
        
        return window.WindowStylesEx.HasFlag(User32.WindowStylesEx.WS_EX_LAYERED | User32.WindowStylesEx.WS_EX_TOOLWINDOW | User32.WindowStylesEx.WS_EX_TOPMOST);
    }
}