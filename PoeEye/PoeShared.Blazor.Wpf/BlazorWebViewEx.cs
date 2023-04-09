using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Media;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.AspNetCore.Components.WebView.Wpf;
using Microsoft.Extensions.FileProviders;
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
                WebView.DefaultBackgroundColor = Color.FromArgb(x.R, x.G, x.B);
            })
            .AddTo(Anchors);
    }

    public InMemoryFileProvider FileProvider { get; } = new();

    public override IFileProvider CreateFileProvider(string contentRootDir)
    {
        var basicProvider = base.CreateFileProvider(contentRootDir);

        return new CompositeFileProvider(FileProvider, basicProvider);
    }
}