namespace PoeShared.Blazor.WinForms.Automation;

public sealed record BlazorWebViewAutomationOptions(
    bool EnableAutomation = false,
    int BrowserDebugPort = 49220);

public interface IBlazorWebViewAutomationOptionsProvider
{
    BlazorWebViewAutomationOptions GetOptions();
}

internal sealed class DefaultBlazorWebViewAutomationOptionsProvider : IBlazorWebViewAutomationOptionsProvider
{
    private static readonly BlazorWebViewAutomationOptions DefaultOptions = new();

    public BlazorWebViewAutomationOptions GetOptions()
    {
        return DefaultOptions;
    }
}
