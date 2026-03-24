namespace PoeShared.Blazor.Wpf.Automation;

/// <summary>
/// Defines the runtime options that control browser automation support for hosted Blazor WebView instances.
/// These options determine whether remote browser automation is exposed and which debug port is used for CDP-compatible tooling.
/// </summary>
/// <param name="EnableAutomation">
/// When <see langword="true"/>, hosted WebView instances expose browser automation capabilities such as remote debugging.
/// When <see langword="false"/>, browser attachment is disabled even though local discovery helpers may still work.
/// </param>
/// <param name="BrowserDebugPort">
/// TCP port used for browser debugging and CDP-compatible automation when automation is enabled.
/// </param>
public sealed record BlazorWebViewAutomationOptions(
    bool EnableAutomation = false,
    int BrowserDebugPort = 49220);

/// <summary>
/// Provides the automation options that should be applied to hosted Blazor WebView instances.
/// </summary>
public interface IBlazorWebViewAutomationOptionsProvider
{
    /// <summary>
    /// Returns the automation options that should be used for newly created hosted WebView instances.
    /// </summary>
    /// <returns>
    /// The resolved automation options for the current application environment.
    /// </returns>
    BlazorWebViewAutomationOptions GetOptions();
}

internal sealed class DefaultBlazorWebViewAutomationOptionsProvider : IBlazorWebViewAutomationOptionsProvider
{
    private static readonly BlazorWebViewAutomationOptions DefaultOptions = new();

    /// <summary>
    /// Returns the default automation options used when no custom provider is registered.
    /// </summary>
    /// <returns>
    /// A shared default options instance with browser automation disabled.
    /// </returns>
    public BlazorWebViewAutomationOptions GetOptions()
    {
        return DefaultOptions;
    }
}
