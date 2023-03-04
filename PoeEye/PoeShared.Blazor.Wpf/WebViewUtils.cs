using System;
using Microsoft.Web.WebView2.Core;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Wpf;

public static class WebViewUtils
{
    private static readonly IFluentLog Log = typeof(WebViewUtils).PrepareLogger();

    public static bool IsInstalled => GetInstallType() != WebViewInstallType.NotInstalled;

    public static string GetAvailableBrowserVersion()
    {
        return CoreWebView2Environment.GetAvailableBrowserVersionString();
    }

    public static WebViewInstallType GetInstallType()
    {
        try
        {
            var webViewVersion = GetAvailableBrowserVersion();
            return GetInstallTypeFromVersion(webViewVersion);
        }
        catch (Exception e)
        {
            Log.Error("Failed to retrieve WebView2 version", e);
            return WebViewInstallType.NotInstalled;
        }
    }

    public static WebViewInstallType GetInstallTypeFromVersion(string version) =>
        version switch
        {
            null => WebViewInstallType.NotInstalled,
            _ when version.Contains("dev") => WebViewInstallType.EdgeChromiumDev,
            _ when version.Contains("beta") => WebViewInstallType.EdgeChromiumBeta,
            _ when version.Contains("canary") => WebViewInstallType.EdgeChromiumCanary,
            _ => !string.IsNullOrEmpty(version) ? WebViewInstallType.WebView2 : WebViewInstallType.NotInstalled
        };
}