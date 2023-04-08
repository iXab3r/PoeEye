using Microsoft.AspNetCore.Components.WebView.Wpf;
using Microsoft.Extensions.FileProviders;

namespace PoeShared.Blazor.Wpf;

public sealed class BlazorWebViewEx : BlazorWebView
{
    public BlazorWebViewEx()
    {
    }

    public InMemoryFileProvider FileProvider { get; } = new();

    public override IFileProvider CreateFileProvider(string contentRootDir)
    {
        var basicProvider = base.CreateFileProvider(contentRootDir);

        return new CompositeFileProvider(FileProvider, basicProvider);
    }
}