#nullable enable

using System;

namespace PoeShared.Blazor.Wpf.Automation;

/// <summary>
/// Defines a stable automation identity for an EyeAuras Blazor window.
/// Implementations expose the window-level identifier that is used to derive stable browser view identifiers.
/// </summary>
public interface IBlazorWindowAutomationIdentity
{
    /// <summary>
    /// Gets or sets the stable automation identifier assigned to the window.
    /// </summary>
    string AutomationId { get; set; }
}

/// <summary>
/// Provides helpers for working with optional <see cref="IBlazorWindowAutomationIdentity"/> support on Blazor windows.
/// </summary>
public static class BlazorWindowAutomationIdentityExtensions
{
    /// <summary>
    /// Returns the stable automation identifier assigned to the specified window when available.
    /// </summary>
    /// <param name="window">
    /// Window whose automation identifier should be resolved.
    /// </param>
    /// <returns>
    /// The stable window automation identifier when the window supports automation identity and the identifier is non-empty;
    /// otherwise <see langword="null"/>.
    /// </returns>
    public static string? GetAutomationId(this IBlazorWindow window)
    {
        ArgumentNullException.ThrowIfNull(window);
        return window is IBlazorWindowAutomationIdentity automationIdentity && !string.IsNullOrWhiteSpace(automationIdentity.AutomationId)
            ? automationIdentity.AutomationId
            : null;
    }

    /// <summary>
    /// Builds a stable browser view automation identifier for the specified window and semantic view role.
    /// </summary>
    /// <param name="window">
    /// Window whose derived browser view automation identifier should be resolved.
    /// </param>
    /// <param name="role">
    /// Semantic role of the target browser view, such as <c>body</c> or <c>titlebar</c>.
    /// </param>
    /// <returns>
    /// The derived browser view automation identifier when the window exposes a non-empty automation identifier;
    /// otherwise <see langword="null"/>.
    /// </returns>
    public static string? GetViewAutomationId(this IBlazorWindow window, BlazorWindowViewRole role)
    {
        ArgumentNullException.ThrowIfNull(window);

        var automationId = window.GetAutomationId();
        return string.IsNullOrWhiteSpace(automationId)
            ? null
            : $"{automationId}/{role.ToAutomationSegment()}";
    }

    /// <summary>
    /// Attempts to assign a stable automation identifier to the specified window.
    /// </summary>
    /// <param name="window">
    /// Window that should receive the automation identifier.
    /// </param>
    /// <param name="automationId">
    /// Requested automation identifier to assign.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the window supports automation identity and the normalized assigned identifier is non-empty;
    /// otherwise <see langword="false"/>.
    /// </returns>
    public static bool TrySetAutomationId(this IBlazorWindow window, string automationId)
    {
        ArgumentNullException.ThrowIfNull(window);
        if (window is not IBlazorWindowAutomationIdentity automationIdentity)
        {
            window.Log.Warn($"Window {window} does not support automation identity assignment");
            return false;
        }

        automationIdentity.AutomationId = automationId?.Trim() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(automationIdentity.AutomationId);
    }
}
