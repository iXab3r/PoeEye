using PInvoke;
using PoeShared.Native;

namespace PoeShared.Scaffolding
{
    public static class WindowHandleExtensions
    {
        /// <summary>
        ///   Checks Visibility, WS_CHILD, WS_EX_TOOLWINDOW and other properties to make sure that this window could be interacted with
        /// </summary>
        /// <returns></returns>
        public static bool IsVisibleAndValid(this IWindowHandle windowHandle, bool excludeMinimized = false)
        {
            if (!windowHandle.IsVisible || excludeMinimized && windowHandle.IsIconic)
            {
                return false;
            }
            
            if (excludeMinimized && windowHandle.ClientBounds.Width <= 0 || windowHandle.ClientBounds.Height <= 0)
            {
                return false;
            }
            
            if (windowHandle.WindowStylesEx.HasFlag(User32.WindowStylesEx.WS_EX_TOOLWINDOW))
            {
                return false;
            }

            if (windowHandle.WindowStyle.HasFlag(User32.WindowStyles.WS_CHILD) || windowHandle.WindowStyle.HasFlag(User32.WindowStyles.WS_POPUP) || windowHandle.WindowStyle.HasFlag(User32.WindowStyles.WS_POPUPWINDOW))
            {
                return false;
            }

            return true;
        }
    }
}