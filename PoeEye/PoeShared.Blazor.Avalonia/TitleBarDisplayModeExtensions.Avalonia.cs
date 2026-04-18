using PoeShared.Blazor.Wpf;

namespace PoeShared.Blazor.Avalonia;

internal static class TitleBarDisplayModeExtensionsAvalonia
{
    public static TitleBarDisplayMode ResolveForAvalonia(this TitleBarDisplayMode titleBarDisplayMode)
    {
        return titleBarDisplayMode == TitleBarDisplayMode.Default
            ? TitleBarDisplayMode.Custom
            : titleBarDisplayMode;
    }
}
