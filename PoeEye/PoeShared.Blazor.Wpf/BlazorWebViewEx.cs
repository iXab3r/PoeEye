using System;
using System.IO;
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
using PoeShared.Scaffolding;
using Color = System.Drawing.Color;

namespace PoeShared.Blazor.Wpf;

public class BlazorWebViewEx : BlazorWebView
{
    protected CompositeDisposable Anchors { get; } = new CompositeDisposable();
    
    public BlazorWebViewEx()
    {
        this.BlazorWebViewInitialized += OnBlazorWebViewInitialized;
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
    }

    public InMemoryFileProvider FileProvider { get; } = new();

    public override IFileProvider CreateFileProvider(string contentRootDir)
    {
        var basicProvider = base.CreateFileProvider(contentRootDir);

        return new CompositeFileProvider(FileProvider, basicProvider);
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
}