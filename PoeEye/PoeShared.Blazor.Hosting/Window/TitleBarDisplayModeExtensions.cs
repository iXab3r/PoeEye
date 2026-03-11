namespace PoeShared.Blazor.Wpf;

public static class TitleBarDisplayModeExtensions
{
    public static TitleBarDisplayMode ResolveForWpf(this TitleBarDisplayMode titleBarDisplayMode)
    {
        return titleBarDisplayMode == TitleBarDisplayMode.Default
            ? TitleBarDisplayMode.System
            : titleBarDisplayMode;
    }

    public static TitleBarDisplayMode ResolveForWinForms(this TitleBarDisplayMode titleBarDisplayMode)
    {
        return titleBarDisplayMode == TitleBarDisplayMode.Default
            ? TitleBarDisplayMode.Custom
            : titleBarDisplayMode;
    }
}
