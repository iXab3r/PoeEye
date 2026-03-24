#nullable enable

namespace PoeShared.Blazor.Wpf.Automation;

/// <summary>
/// Describes a registered Blazor WebView host that belongs to an EyeAuras window.
/// This snapshot is intended for diagnostics, browser automation discovery, and tooling that needs
/// to reason about the currently available browser views without holding a live UI reference.
/// </summary>
/// <param name="WindowAutomationId">
/// Stable automation identifier of the owning EyeAuras window.
/// Multiple browser views can share the same window automation identifier when they belong to the same logical window.
/// </param>
/// <param name="ViewAutomationId">
/// Stable automation identifier of the concrete browser view, typically composed from the window automation id
/// and a semantic role such as <c>body</c> or <c>titlebar</c>.
/// </param>
/// <param name="Role">
/// Semantic role of the browser view within the owning window, for example <c>body</c> or <c>titlebar</c>.
/// </param>
/// <param name="IsReady">
/// Indicates whether the underlying WebView2 instance is fully initialized and can be inspected or automated.
/// </param>
/// <param name="IsVisible">
/// Indicates whether the hosting WPF control is currently visible to the user.
/// </param>
/// <param name="Title">
/// Current document title reported by the hosted WebView when available.
/// </param>
/// <param name="CurrentUrl">
/// Current URL loaded inside the hosted WebView when available.
/// </param>
/// <param name="ViewType">
/// Fully qualified CLR type name of the Blazor view currently hosted by the control when available.
/// </param>
/// <param name="DataContextType">
/// Fully qualified CLR type name of the EyeAuras data context currently bound to the hosted view when available.
/// </param>
/// <param name="HasUnhandledException">
/// Indicates whether the hosting control has recorded an unhandled exception for the current view instance.
/// </param>
public sealed record BlazorWindowViewDescriptor(
    string WindowAutomationId,
    string ViewAutomationId,
    string Role,
    bool IsReady,
    bool IsVisible,
    string? Title,
    string? CurrentUrl,
    string? ViewType,
    string? DataContextType,
    bool HasUnhandledException);
