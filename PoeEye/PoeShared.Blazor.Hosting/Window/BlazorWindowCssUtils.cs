using System;
using System.Globalization;
using System.Windows;
using PoeShared.Native;

namespace PoeShared.Blazor.Wpf;

internal static class BlazorWindowCssUtils
{
    public const string WindowCornerRadiusVariable = "--blazor-window-corner-radius";

    public static string BuildSystemCornerRadiusStyle(IBlazorWindow window)
    {
        if (window == null || window.TitleBarDisplayMode != TitleBarDisplayMode.System || window.WindowState == WindowState.Maximized)
        {
            return null;
        }

        var windowHandle = window.GetWindowHandle();
        var radius = windowHandle != IntPtr.Zero
            ? UnsafeNative.GetApproximateWindowCornerRadius(windowHandle)
            : UnsafeNative.IsWindows10OrGreater(22000) ? 8 : 0;
        if (radius <= 0)
        {
            return null;
        }

        return $"{WindowCornerRadiusVariable}: {radius.ToString(CultureInfo.InvariantCulture)}px;";
    }
}
