namespace PoeShared.Blazor.Services;

/// <summary>
/// Browser keyboard shortcut descriptor used by <see cref="IJsPoeBlazorUtils.SuppressWellKnownBrowserShortcuts"/>.
/// </summary>
public sealed record JsBrowserShortcut(
    string Key,
    bool CtrlKey = false,
    bool ShiftKey = false,
    bool AltKey = false,
    bool MetaKey = false);
