using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.AspNetCore.Components.WebView.Wpf;
using Microsoft.Extensions.FileProviders;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Core.Raw;
using Microsoft.Web.WebView2.Wpf;
using PoeShared.Blazor.Wpf.Services;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using Color = System.Drawing.Color;

namespace PoeShared.Blazor.Wpf;

public class BlazorWebViewEx : BlazorWebView, IDisposable
{
    private static readonly IFluentLog Log = typeof(BlazorWebViewEx).PrepareLogger();
    private static readonly ConcurrentDictionary<string, IFileProvider> StaticFilesProvidersByPath = new();
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
            throw new InvalidStateException($"It is expected than inner WebView2 will be hosted as WPF version of WebView2 ({typeof(WebView2)}) {WebViewTemplateChildName} inside visual tree of {this}, got other control instead: {frameworkElementFactory.Type}");
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

        
        webView2Ex = (WebView2Ex)Template.FindName(WebViewTemplateChildName, this) ?? throw new InvalidStateException($"Failed to find web view control {WebViewTemplateChildName}");
        
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
        e.EnvironmentOptions = new CoreWebView2EnvironmentOptions()
        {
            AdditionalBrowserArguments = "",
        };
    }
    
    private void OnBlazorWebViewInitialized(object sender, BlazorWebViewInitializedEventArgs e)
    {
        this.Observe(BackgroundProperty, x => Background)
            .Select(x => x is SolidColorBrush solidColorBrush ? solidColorBrush.Color : default)
            .Subscribe(x => { WebView.DefaultBackgroundColor = Color.FromArgb(x.A, x.R, x.G, x.B); })
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

    public override IFileProvider CreateFileProvider(string contentRootDir)
    {
        var contentRoot = new DirectoryInfo(contentRootDir);
        Log.Debug($"Initializing content provider @ {contentRoot}");
        var staticFilesProvider = StaticFilesProvidersByPath.GetOrAdd(contentRoot.FullName, _ => new CachingFileProvider(contentRoot));
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
}