using System;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace PoeShared.Blazor.Wpf;

internal sealed class CoreWebView2Accessor : ICoreWebView2Accessor
{
    public WebView2CompositionControl WebView2 { get; }

    public CoreWebView2Accessor(WebView2CompositionControl webView2)
    {
        WebView2 = webView2;
    }

    public CoreWebView2 CoreWebView2 => WebView2.CoreWebView2 ?? throw new InvalidOperationException("CoreWebView2 is not initialized");
}