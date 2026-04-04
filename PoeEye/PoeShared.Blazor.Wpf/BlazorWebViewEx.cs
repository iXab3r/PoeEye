using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.AspNetCore.Components.WebView.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Core.Raw;
using Microsoft.Web.WebView2.Wpf;
using PoeShared.Blazor.Wpf.Automation;
using PoeShared.Blazor.Wpf.Scaffolding;
using PoeShared.Blazor.Wpf.Services;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using Color = System.Drawing.Color;
using CompositeFileProvider = Microsoft.Extensions.FileProviders.CompositeFileProvider;
using DirectoryInfo = System.IO.DirectoryInfo;

namespace PoeShared.Blazor.Wpf;

public class BlazorWebViewEx : BlazorWebView, IDisposable
{
    private static readonly IFluentLog Log = typeof(BlazorWebViewEx).PrepareLogger();
    private const string WebViewTemplateChildName = "WebView";

    protected CompositeDisposable Anchors { get; } = new();
    private WebView2Ex webView2Ex;
    private readonly ProxyFileProvider proxyFileProvider = new();

    public BlazorWebViewEx()
    {
        var existingTemplate = Template;
        if (existingTemplate?.VisualTree is not { } frameworkElementFactory)
        {
            throw new InvalidStateException($"It is expected than inner WebView2 will be hosted as {WebViewTemplateChildName} inside visual tree of {this}, got nothing instead");
        }

        if (frameworkElementFactory.Type != typeof(WebView2))
        {
            throw new InvalidStateException(
                $"It is expected than inner WebView2 will be hosted as WPF version of WebView2 ({typeof(WebView2)}) {WebViewTemplateChildName} inside visual tree of {this}, got other control instead: {frameworkElementFactory.Type}");
        }

        Template = new ControlTemplate
        {
            VisualTree = new FrameworkElementFactory(typeof(WebView2Ex), WebViewTemplateChildName)
        };

        this.BlazorWebViewInitializing += OnBlazorWebViewInitializing;
        this.BlazorWebViewInitialized += OnBlazorWebViewInitialized;
    }

    public IFileProvider FileProvider
    {
        get => proxyFileProvider.FileProvider;
        set => proxyFileProvider.FileProvider = value;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();


        webView2Ex = (WebView2Ex) Template.FindName(WebViewTemplateChildName, this) ?? throw new InvalidStateException($"Failed to find web view control {WebViewTemplateChildName}");

        /*
         * This is an attempt to fix flickering problem described here:
         * https://github.com/MicrosoftEdge/WebView2Feedback/issues/1412
         * and here
         * https://www.cnblogs.com/liwuqingxin/p/16266683.html
         * The idea is to temporarily relocate webview until it will be fully loaded
         */
        webView2Ex.RenderTransform = new TranslateTransform(-int.MaxValue, -int.MaxValue);
    }

    private void OnBlazorWebViewInitializing(object sender, BlazorWebViewInitializingEventArgs e)
    {
        e.EnvironmentOptions ??= new CoreWebView2EnvironmentOptions();

        var requestedAdditionalBrowserArguments = e.EnvironmentOptions.AdditionalBrowserArguments ?? string.Empty;
        var automationOptionsProvider = Services?.GetService<IBlazorWebViewAutomationOptionsProvider>();
        var automationOptions = automationOptionsProvider?.GetOptions() ?? new BlazorWebViewAutomationOptions();
        Log.Info(
            $"Initializing WebView2 automation: Provider={automationOptionsProvider?.GetType().FullName ?? "<null>"}, EnableAutomation={automationOptions.EnableAutomation}, BrowserDebugPort={automationOptions.BrowserDebugPort}, RequestedAdditionalBrowserArguments='{requestedAdditionalBrowserArguments}'");
        if (automationOptions.EnableAutomation)
        {
            if (!automationOptions.BrowserDebugPort.IsBetween(1, 65535, true))
            {
                Log.Warn($"WebView2 Remote-debugging port is {automationOptions.BrowserDebugPort}, it is not between 1 and 65535, ignoring");
            }
            else
            {
                requestedAdditionalBrowserArguments = AppendBrowserArgument(requestedAdditionalBrowserArguments, $"--remote-debugging-port={automationOptions.BrowserDebugPort}");
            }
        }

        var environmentController = Services?.GetService<IWebView2EnvironmentController>();
        if (environmentController == null)
        {
            e.EnvironmentOptions.AdditionalBrowserArguments = requestedAdditionalBrowserArguments;
            Log.Info($"WebView2 environment controller is not registered, using browser arguments '{requestedAdditionalBrowserArguments}'");
            return;
        }

        var resolution = environmentController.ApplyRequestedSpec(new WebView2EnvironmentSpec(
            BrowserExecutableFolder: e.BrowserExecutableFolder,
            AdditionalBrowserArguments: requestedAdditionalBrowserArguments));

        e.BrowserExecutableFolder = resolution.EffectiveSpec.BrowserExecutableFolder;
        e.EnvironmentOptions.AdditionalBrowserArguments = resolution.EffectiveSpec.AdditionalBrowserArguments;

        if (resolution.RestartRequired)
        {
            Log.Warn($"WebView2 environment restart is required: {resolution.Message}");
            return;
        }

        Log.Info($"WebView2 environment resolved: {resolution.Message}");
    }

    private void OnBlazorWebViewInitialized(object sender, BlazorWebViewInitializedEventArgs e)
    {
        TryApplyDefaultBackgroundColor(e.WebView, CreateWebViewBackgroundColor(Background));

        this.Observe(BackgroundProperty)
            .Select(_ => CreateWebViewBackgroundColor(Background))
            .DistinctUntilChanged()
            .Subscribe(color => TryApplyDefaultBackgroundColor(e.WebView, color))
            .AddTo(Anchors);

        e.WebView.GotFocus += WebViewOnGotFocus;
        e.WebView.LostFocus += WebViewOnLostFocus;
        e.WebView.NavigationCompleted += WebViewOnNavigationCompleted;
        e.WebView.CoreWebView2.PermissionRequested += CoreWebView2OnPermissionRequested;
#if !DEBUG
        e.WebView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false; //disables Ctrl+F, Ctrl+P, etc. DOES NOT DISABLE Ctrl+A/C/V
#endif
        e.WebView.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;
        e.WebView.CoreWebView2.Settings.IsPasswordAutosaveEnabled = false;
        e.WebView.PreviewKeyDown += WebViewOnPreviewKeyDown;
        e.WebView.CoreWebView2.WebMessageReceived += CoreWebView2OnWebMessageReceived;
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

    private void CoreWebView2OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (e.AdditionalObjects != null)
        {
        }
    }

    private void WebViewOnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        //can intercept/handle keys here via WebView2 CoreWebView2Controller_AcceleratorKeyPressed
    }

    private void WebViewOnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        webView2Ex.RenderTransform = new TranslateTransform(0, 0);
    }

    private void WebViewOnLostFocus(object sender, RoutedEventArgs e)
    {
    }

    private void WebViewOnGotFocus(object sender, RoutedEventArgs e)
    {
    }

    private void CoreWebView2OnPermissionRequested(object sender, CoreWebView2PermissionRequestedEventArgs e)
    {
        Log.Debug($"Permission requested: {e.PermissionKind}, state: {e.State}");
        e.State = CoreWebView2PermissionState.Allow;
    }

    internal static Color CreateWebViewBackgroundColor(Brush background)
    {
        var mediaColor = background is SolidColorBrush solidColorBrush
            ? solidColorBrush.Color
            : default;
        return NormalizeWebViewBackgroundColor(mediaColor);
    }

    internal static Color NormalizeWebViewBackgroundColor(System.Windows.Media.Color mediaColor)
    {
        // WebView2 only accepts fully transparent or fully opaque colors.
        return mediaColor.A switch
        {
            0 => Color.Transparent,
            byte.MaxValue => Color.FromArgb(mediaColor.A, mediaColor.R, mediaColor.G, mediaColor.B),
            _ => Color.FromArgb(byte.MaxValue, mediaColor.R, mediaColor.G, mediaColor.B)
        };
    }

    private void TryApplyDefaultBackgroundColor(WebView2 webView, Color color)
    {
        try
        {
            webView.DefaultBackgroundColor = color;
            Log.Debug($"Applied WebView background color {color}");
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException or ObjectDisposedException or COMException)
        {
            Log.Warn($"Failed to apply WebView background color {color}. WebView will continue with its existing background.", e);
        }
    }

    public override IFileProvider CreateFileProvider(string contentRootDir)
    {
        var contentRoot = new DirectoryInfo(contentRootDir);
        Log.Debug($"Initializing content provider @ {contentRoot}");
        var staticFilesProvider = CachingPhysicalFileProvider.GetOrAdd(new DirectoryInfo(contentRoot.FullName));
        return new CompositeFileProvider(proxyFileProvider, staticFilesProvider);
    }

    public async Task<BitmapSource> TakeScreenshotAsBitmapSource()
    {
        using var imageStream = new MemoryStream();
        await WebView.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, imageStream);
        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.StreamSource = imageStream;
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.EndInit();
        return bitmapImage;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Anchors?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private static string AppendBrowserArgument(string existingArguments, string argument)
    {
        if (string.IsNullOrWhiteSpace(argument))
        {
            return existingArguments ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(existingArguments))
        {
            return argument;
        }

        var existing = existingArguments.Trim();
        if (existing.Contains(argument, StringComparison.OrdinalIgnoreCase))
        {
            return existing;
        }

        return $"{existing} {argument}";
    }
}
