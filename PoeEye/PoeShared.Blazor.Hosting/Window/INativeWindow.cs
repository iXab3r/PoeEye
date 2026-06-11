using System;
using System.Windows;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Wpf;

/// <summary>
/// Defines the contract for a thread-safe native WPF window which hosts arbitrary WPF content - no Blazor/WebView2 involvement.
/// <para>
/// Follows the established windowing model: each window owns a dedicated dispatcher, all changes flow through
/// a single command channel and properties are mirrored via timestamped containers (last-wins).
/// The inner WPF window is created lazily on first Show/ShowDialog.
/// </para>
/// <para>
/// <see cref="IBlazorWindow"/> is a specialization of this contract which hosts Blazor content instead of caller-supplied WPF content.
/// </para>
/// </summary>
public interface INativeWindow : IBlazorWindowController, IDisposableReactiveObject, IBlazorWindowNativeController
{
    /// <summary>
    /// Gets or sets the factory which produces the WPF content hosted inside the window.
    /// <para>
    /// Threading contract: the factory is ALWAYS invoked on the window's own UI (dispatcher) thread, never on the
    /// caller thread - WPF elements have thread affinity, so the content must be created inside the factory rather
    /// than upfront. The factory is invoked lazily: the first time when the inner WPF window is created (on first
    /// Show/ShowDialog), and again whenever the property is re-assigned afterwards. Re-assigning after the window
    /// is shown is supported - updates are serialized through the window command queue (last-wins) and the produced
    /// element replaces the previous content. Setting the property to null clears the hosted content.
    /// </para>
    /// <para>
    /// Window implementations which host non-WPF content (e.g. Blazor windows) do not support this property
    /// and throw <see cref="NotSupportedException"/> when a non-null value is assigned.
    /// </para>
    /// </summary>
    Func<INativeWindow, UIElement> ContentFactory
    {
        get => null;
        set => throw new NotSupportedException("This window implementation does not support arbitrary WPF content");
    }
}
