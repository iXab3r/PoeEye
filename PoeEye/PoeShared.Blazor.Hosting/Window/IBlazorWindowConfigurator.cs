namespace PoeShared.Blazor.Wpf;

/// <summary>
/// Applies container-specific defaults to a newly constructed <see cref="IBlazorWindow"/>.
/// </summary>
/// <remarks>
/// <para>
/// This hook exists for scenarios where a container wants every window it creates to receive the same
/// setup automatically, without forcing each caller to remember extra initialization steps.
/// </para>
/// <para>
/// A configurator is resolved from the current container as an optional dependency of the concrete window
/// implementation and is invoked during window construction.
/// </para>
/// <para>
/// Typical use cases include:
/// </para>
/// <list type="bullet">
/// <item>
/// applying a scoped <see cref="IBlazorWindow.Container"/> for view resolution
/// </item>
/// <item>
/// registering additional file providers for static assets
/// </item>
/// <item>
/// attaching container-specific defaults such as automation ids, theme helpers, or diagnostics hooks
/// </item>
/// </list>
/// <para>
/// Implementations should keep this method lightweight and deterministic. Prefer setting defaults and attaching
/// resources over performing expensive work, showing the window, or triggering behavior that depends on the
/// native window already being visible.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// internal sealed class ScriptBlazorWindowConfigurator : IBlazorWindowConfigurator
/// {
///     private readonly IUnityContainer scriptContainer;
///     private readonly IFileProvider scriptFileProvider;
///
///     public ScriptBlazorWindowConfigurator(
///         IUnityContainer scriptContainer,
///         IFileProvider scriptFileProvider)
///     {
///         this.scriptContainer = scriptContainer;
///         this.scriptFileProvider = scriptFileProvider;
///     }
///
///     public void Configure(IBlazorWindow window)
///     {
///         window.Container = scriptContainer;
///         window.RegisterFileProvider(scriptFileProvider).AddTo(window.Anchors);
///     }
/// }
///
/// // Register in a child container so every window resolved from that scope inherits the setup.
/// childContainer.RegisterSingleton&lt;ScriptBlazorWindowConfigurator&gt;(typeof(IBlazorWindowConfigurator));
///
/// // Later:
/// var window = childContainer.Resolve&lt;IBlazorWindow&gt;();
/// </code>
/// </example>
/// <example>
/// <code>
/// internal sealed class DebugWindowConfigurator : IBlazorWindowConfigurator
/// {
///     public void Configure(IBlazorWindow window)
///     {
///         window.TitleBarDisplayMode = TitleBarDisplayMode.Default;
///         window.ShowInTaskbar = false;
///         window.IsDebugMode = true;
///     }
/// }
/// </code>
/// </example>
public interface IBlazorWindowConfigurator
{
    /// <summary>
    /// Applies defaults to the specified <paramref name="window"/> before it is handed back to callers.
    /// </summary>
    /// <param name="window">
    /// The newly constructed window instance that should receive container-specific initialization.
    /// </param>
    /// <remarks>
    /// This method may be called for every new window created from a container scope. Implementations should
    /// assume that callers may still override any configured values afterwards.
    /// </remarks>
    void Configure(IBlazorWindow window);
}
