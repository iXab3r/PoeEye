using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.FileProviders;
using Microsoft.Web.WebView2.Core;
using PoeShared.Blazor.Wpf.Services;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using CompositeFileProvider = Microsoft.Extensions.FileProviders.CompositeFileProvider;
using DirectoryInfo = System.IO.DirectoryInfo;

namespace PoeShared.Blazor.WinForms;

public sealed class BlazorWebViewEx : BlazorWebView
{
    private static readonly IFluentLog Log = typeof(BlazorWebViewEx).PrepareLogger();

    private readonly ProxyFileProvider proxyFileProvider = new();

    public BlazorWebViewEx()
    {
        BlazorWebViewInitializing += OnBlazorWebViewInitializing;
        BlazorWebViewInitialized += OnBlazorWebViewInitialized;
    }

    public IFileProvider? FileProvider
    {
        get => proxyFileProvider.FileProvider;
        set => proxyFileProvider.FileProvider = value;
    }

    public override IFileProvider CreateFileProvider(string contentRootDir)
    {
        var contentRoot = new DirectoryInfo(contentRootDir);
        Log.Debug($"Initializing content provider @ {contentRoot}");
        var staticFilesProvider = CachingPhysicalFileProvider.GetOrAdd(contentRoot);
        return new CompositeFileProvider(proxyFileProvider, staticFilesProvider);
    }

    private static void OnBlazorWebViewInitializing(object? sender, BlazorWebViewInitializingEventArgs e)
    {
        e.EnvironmentOptions = new CoreWebView2EnvironmentOptions
        {
            AdditionalBrowserArguments = string.Empty,
        };
    }

    private void OnBlazorWebViewInitialized(object? sender, BlazorWebViewInitializedEventArgs e)
    {
        e.WebView.CoreWebView2.PermissionRequested += CoreWebView2OnPermissionRequested;
#if !DEBUG
        e.WebView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
#endif
        e.WebView.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;
        e.WebView.CoreWebView2.Settings.IsPasswordAutosaveEnabled = false;

        var drives = LogicalDriveListProvider.Instance.Drives.Items.ToArray();
        Log.Info($"Updating virtual mappings, drives: {drives.Select(x => x.FullName).DumpToString()}");
        foreach (var rootDirectory in drives)
        {
            var driveLetter = rootDirectory.GetDriveLetter();
            try
            {
                if (string.IsNullOrEmpty(driveLetter) || !rootDirectory.Exists)
                {
                    Log.Warn($"Could not update virtual mapping for drive {rootDirectory.FullName}, exists: {rootDirectory.Exists} (drive removed/renamed?)");
                    continue;
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"Failed to update virtual mapping for drive {rootDirectory.FullName}", ex);
                continue;
            }

            e.WebView.CoreWebView2.SetVirtualHostNameToFolderMapping(driveLetter, rootDirectory.FullName, CoreWebView2HostResourceAccessKind.Allow);
        }
    }

    private static void CoreWebView2OnPermissionRequested(object? sender, CoreWebView2PermissionRequestedEventArgs e)
    {
        Log.Debug($"Permission requested: {e.PermissionKind}, state: {e.State}");
        e.State = CoreWebView2PermissionState.Allow;
    }
}
