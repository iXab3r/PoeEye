namespace PoeShared.Blazor.Services;

/// <summary>
/// Options for DOM-level browser shortcut suppression.
/// </summary>
public sealed class JsBrowserShortcutSuppressionOptions
{
    /// <summary>
    /// Shortcuts to suppress. Leave null to use the shared WebView/browser defaults.
    /// </summary>
    public JsBrowserShortcut[]? Shortcuts { get; init; }

    /// <summary>
    /// Allows Ctrl+A to keep normal text selection behavior inside editable elements.
    /// </summary>
    public bool AllowCtrlAInEditable { get; init; } = true;

    /// <summary>
    /// Selector for DOM subtrees where shortcut suppression should be skipped.
    /// </summary>
    public string AllowSelector { get; init; } = "[data-poe-browser-shortcuts=\"allow\"]";

    /// <summary>
    /// Calls preventDefault for matched shortcuts.
    /// </summary>
    public bool PreventDefault { get; init; } = true;

    /// <summary>
    /// Calls stopPropagation for matched shortcuts. Disabled by default so Blazor handlers still see the event.
    /// </summary>
    public bool StopPropagation { get; init; }
}
