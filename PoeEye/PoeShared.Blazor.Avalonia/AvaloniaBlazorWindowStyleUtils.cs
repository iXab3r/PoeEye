using System.Globalization;
using PoeShared.Blazor.Wpf;
using PoeShared.Native;
using WpfColor = System.Windows.Media.Color;
using WpfThickness = System.Windows.Thickness;
using WpfWindowState = System.Windows.WindowState;

namespace PoeShared.Blazor.Avalonia;

internal static class AvaloniaBlazorWindowStyleUtils
{
    private const string WindowCornerRadiusVariable = "--blazor-window-corner-radius";

    public static string BuildCssColor(WpfColor color)
    {
        var alpha = (color.A / 255d).ToString("0.###", CultureInfo.InvariantCulture);
        return $"rgba({color.R}, {color.G}, {color.B}, {alpha})";
    }

    public static string BuildCssPadding(WpfThickness thickness)
    {
        return $"{thickness.Top.ToString(CultureInfo.InvariantCulture)}px " +
               $"{thickness.Right.ToString(CultureInfo.InvariantCulture)}px " +
               $"{thickness.Bottom.ToString(CultureInfo.InvariantCulture)}px " +
               $"{thickness.Left.ToString(CultureInfo.InvariantCulture)}px";
    }

    public static string? BuildSystemCornerRadiusStyle(IBlazorWindow window, TitleBarDisplayMode effectiveTitleBarDisplayMode)
    {
        if (window == null || effectiveTitleBarDisplayMode != TitleBarDisplayMode.System || window.WindowState == WpfWindowState.Maximized)
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
