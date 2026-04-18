using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Web.WebView2.Core;
using PoeShared.Blazor.Wpf.Services;
using PoeShared.Blazor.WinForms.Automation;
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
        e.EnvironmentOptions = new CoreWebView2EnvironmentOptions();

        if (sender is not BlazorWebViewEx webView)
        {
            return;
        }

        var automationOptionsProvider = webView.Services?.GetService<IBlazorWebViewAutomationOptionsProvider>();
        var automationOptions = automationOptionsProvider?.GetOptions() ?? new BlazorWebViewAutomationOptions();
        if (!automationOptions.EnableAutomation || !automationOptions.BrowserDebugPort.IsBetween(1, 65535, inclusive: true))
        {
            return;
        }

        e.EnvironmentOptions.AdditionalBrowserArguments = AppendBrowserArgument(
            e.EnvironmentOptions.AdditionalBrowserArguments,
            $"--remote-debugging-port={automationOptions.BrowserDebugPort}");
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

    private static string AppendBrowserArgument(string? existingArguments, string newArgument)
    {
        return string.IsNullOrWhiteSpace(existingArguments)
            ? newArgument
            : existingArguments.Contains(newArgument, StringComparison.OrdinalIgnoreCase)
                ? existingArguments
                : $"{existingArguments} {newArgument}";
    }
}
