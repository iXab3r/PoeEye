using System;
using JetBrains.Annotations;
using Microsoft.Web.WebView2.Core;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using PropertyBinder;

namespace PoeShared.Blazor.Wpf;

internal sealed class WebViewAccessor : DisposableReactiveObject, IWebViewAccessor
{
    private static readonly Binder<WebViewAccessor> Binder = new();

    private static readonly Lazy<WebViewAccessor> InstanceSupplier = new();
    private static readonly IFluentLog Log = typeof(WebViewAccessor).PrepareLogger();

    static WebViewAccessor()
    {
        Binder.Bind(x => GetInstallTypeFromVersion(x.AvailableBrowserVersion)).To(x => x.InstallType);
        Binder.Bind(x => x.InstallType != WebViewInstallType.NotInstalled).To(x => x.IsInstalled);
    }

    public WebViewAccessor()
    {
        Refresh();
        Binder.Attach(this).AddTo(Anchors);
    }

    public static WebViewAccessor Instance => InstanceSupplier.Value;

    public bool IsInstalled { get; [UsedImplicitly] set; }

    public string AvailableBrowserVersion { get; [UsedImplicitly] private set; }

    public WebViewInstallType InstallType { get; [UsedImplicitly] private set; }

    public void Refresh()
    {
        AvailableBrowserVersion = GetAvailableBrowserVersionString();
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