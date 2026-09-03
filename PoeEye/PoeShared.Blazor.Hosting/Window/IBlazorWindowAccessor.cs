using System;

namespace PoeShared.Blazor.Wpf;

/// <summary>
/// This interface could be used to access current Blazor Window from inside its context
/// </summary>
public interface IBlazorWindowAccessor
{
    IBlazorWindow Window { get; }

    /// <summary>
    /// Gets whether focused content currently owns window-level keyboard shortcuts.
    /// </summary>
    bool AreWindowShortcutsSuppressed { get; }

    /// <summary>
    /// Suppresses window-level keyboard shortcuts until the returned lease is disposed.
    /// </summary>
    IDisposable SuppressWindowShortcuts();
}
