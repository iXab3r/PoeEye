using System;
using Microsoft.Web.WebView2.Core;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Wpf;

internal sealed class WebViewAccessor : DisposableReactiveObject, IWebViewAccessor
{
    private static readonly Lazy<WebViewAccessor> InstanceSupplier = new();
    private static readonly IFluentLog Log = typeof(WebViewAccessor).PrepareLogger();

    public WebViewAccessor()
    {
        Refresh();
    }

    public static WebViewAccessor Instance => InstanceSupplier.Value;

    public bool IsInstalled { get; private set; }

    public string AvailableBrowserVersion { get; private set; }

    public WebViewInstallType InstallType { get; private set; }

    public void Refresh()
    {
        AvailableBrowserVersion = GetAvailableBrowserVersionString();
        InstallType = GetInstallTypeFromVersion(AvailableBrowserVersion);
        IsInstalled = InstallType != WebViewInstallType.NotInstalled;
    }

    public static WebViewInstallType GetInstallTypeFromVersion(string version)
    {
        return version switch
        {
            null => WebViewInstallType.NotInstalled,
            _ when version.Contains("dev") => WebViewInstallType.EdgeChromiumDev,
            _ when version.Contains("beta") => WebViewInstallType.EdgeChromiumBeta,
            _ when version.Contains("canary") => WebViewInstallType.EdgeChromiumCanary,
            _ => !string.IsNullOrEmpty(version) ? WebViewInstallType.WebView2 : WebViewInstallType.NotInstalled
        };
    }

    private static string GetAvailableBrowserVersionString()
    {
        try
        {
            return CoreWebView2Environment.GetAvailableBrowserVersionString();
        }
        catch (Exception e)
        {
            Log.Error("Failed to retrieve WebView2 version", e);
            return default;
        }
    }
}
