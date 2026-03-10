using System;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using PoeShared.Blazor.Wpf;

namespace PoeShared.Blazor.WinForms;

internal sealed class CoreWebView2Accessor : ICoreWebView2Accessor
{
    private readonly Func<WebView2?> webViewSupplier;

    public CoreWebView2Accessor(Func<WebView2?> webViewSupplier)
    {
        this.webViewSupplier = webViewSupplier;
    }

    public CoreWebView2 CoreWebView2 => webViewSupplier()?.CoreWebView2 ?? throw new InvalidOperationException("CoreWebView2 is not initialized");
}
