namespace PoeShared.Blazor;

/// <summary>
/// Contains the well-known HTML attribute names that EyeAuras writes into rendered Blazor DOM nodes
/// to support browser automation, diagnostics, and stable view discovery across WebView hosts.
/// </summary>
public static class WellKnownAutomationIds
{
    /// <summary>
    /// Gets the attribute name that stores the stable automation identifier of a rendered EyeAuras element.
    /// This value is intended to be consumed by test tooling, browser automation, and diagnostics when a
    /// specific component instance needs to be located reliably inside the DOM.
    /// </summary>
    public const string AutomationIdAttribute = "data-ea-automation-id";

    /// <summary>
    /// Gets the attribute name that stores the fully qualified runtime type name of the current Blazor data context.
    /// This attribute is primarily meant for diagnostics and inspection scenarios where automation needs to understand
    /// which .NET object type produced a given DOM fragment.
    /// </summary>
    public const string DataContextTypeAttribute = "data-ea-datacontext-type";

    /// <summary>
    /// Gets the attribute name that identifies the owning EyeAuras window instance for a rendered DOM fragment.
    /// The value is stable for the lifetime of the window and allows automation tooling to group elements by their
    /// parent EyeAuras window when multiple WebView hosts are active.
    /// </summary>
    public const string WindowIdAttribute = "data-ea-window-id";

    /// <summary>
    /// Gets the attribute name that identifies the hosting EyeAuras browser view, such as a body or title bar view.
    /// This value is used as the stable top-level selector for browser automation workflows that need to attach to
    /// a specific WebView surface before interacting with nested automation ids.
    /// </summary>
    public const string ViewIdAttribute = "data-ea-view-id";

    /// <summary>
    /// Gets the attribute name that describes the semantic role of the hosting browser view.
    /// Typical values include roles such as <c>body</c> or <c>titlebar</c>, allowing automation and diagnostics
    /// to distinguish different WebView surfaces that belong to the same logical EyeAuras window.
    /// </summary>
    public const string ViewRoleAttribute = "data-ea-view-role";
}
