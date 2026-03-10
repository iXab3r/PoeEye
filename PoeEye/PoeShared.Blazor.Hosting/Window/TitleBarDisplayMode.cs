namespace PoeShared.Blazor.Wpf;

/// <summary>
/// Specifies the display mode for the title bar in a Blazor-based window,
/// providing options for native, embedded, or hidden configurations.
/// </summary>
public enum TitleBarDisplayMode
{
    /// <summary>
    /// Uses the default title bar mode, which relies on the operating system's native title bar.
    /// This is the standard setting that maximizes compatibility with the OS.
    /// </summary>
    Default,

    /// <summary>
    /// Displays the native operating system title bar, providing a familiar look and feel
    /// with standard window controls such as close, minimize, and maximize.
    /// </summary>
    System,

    /// <summary>
    /// Renders a custom title bar within the Blazor environment. This mode offers
    /// maximum flexibility for customization, allowing you to design and control the
    /// title bar’s appearance and functionality within the Blazor UI.
    /// </summary>
    Custom,

    /// <summary>
    /// Hides the title bar entirely, creating a frameless window with no visible title bar or controls.
    /// This mode can be used for immersive experiences or for windows that require full customization.
    /// </summary>
    None
}