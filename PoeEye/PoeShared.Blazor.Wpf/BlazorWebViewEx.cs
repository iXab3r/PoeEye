using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.AspNetCore.Components.WebView.Wpf;
using Microsoft.Extensions.FileProviders;
using Microsoft.Web.WebView2.Core;
using PoeShared.Blazor.Wpf.Services;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using Color = System.Drawing.Color;

namespace PoeShared.Blazor.Wpf;

public class BlazorWebViewEx : BlazorWebView, IDisposable
{
    private static readonly IFluentLog Log = typeof(BlazorWebViewEx).PrepareLogger();
    private static readonly ConcurrentDictionary<string, IFileProvider> StaticFilesProvidersByPath = new();

    protected CompositeDisposable Anchors { get; } = new();
    
    public BlazorWebViewEx()
    {
        this.BlazorWebViewInitializing += OnBlazorWebViewInitializing;
        this.BlazorWebViewInitialized += OnBlazorWebViewInitialized;
    }

    private void OnBlazorWebViewInitializing(object sender, BlazorWebViewInitializingEventArgs e)
    {
        e.EnvironmentOptions = new CoreWebView2EnvironmentOptions()
        {
            AdditionalBrowserArguments = ""
        };
    }

    private void OnBlazorWebViewInitialized(object sender, BlazorWebViewInitializedEventArgs e)
    {
        this.Observe(BackgroundProperty, x => Background)
            .Select(x => x is SolidColorBrush solidColorBrush ? solidColorBrush.Color : default)
            .Subscribe(x =>
            {
                WebView.DefaultBackgroundColor = Color.FromArgb(x.A, x.R, x.G, x.B);
            })
            .AddTo(Anchors);
        e.WebView.GotFocus += WebViewOnGotFocus;
        e.WebView.LostFocus += WebViewOnLostFocus;

        var drives = LogicalDriveListProvider.Instance.Drives.Items.ToArray();
        foreach (var rootDirectory in drives)
        {
            var driveLetter = rootDirectory.GetDriveLetter();
            if (string.IsNullOrEmpty(driveLetter))
            {
                continue;
            }
            e.WebView.CoreWebView2.SetVirtualHostNameToFolderMapping(driveLetter, rootDirectory.FullName, CoreWebView2HostResourceAccessKind.Allow);
        }
    }

    private void WebViewOnLostFocus(object sender, RoutedEventArgs e)
    {
    }

    private void WebViewOnGotFocus(object sender, RoutedEventArgs e)
    {
    }

    public InMemoryFileProvider FileProvider { get; } = new();

    public override IFileProvider CreateFileProvider(string contentRootDir)
    {
        var contentRoot = new DirectoryInfo(contentRootDir);
        Log.Info($"Initializing content provider @ {contentRoot}");
        var staticFilesProvider = StaticFilesProvidersByPath.GetOrAdd(contentRoot.FullName, _ => new CachingFileProvider(contentRoot));
        return new CompositeFileProvider(FileProvider, staticFilesProvider);
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