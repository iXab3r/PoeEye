using System;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Wpf.Services;

/// <summary>
/// This class is needed to allow to reload WebView.
/// The problem is that the current implementation of BlazorWebView allows to set ServiceProvider only once - before WebView is initialized
/// This ServiceProvider is then propagated to WebViewManager and is used in AttachToPageAsync to create scoped version of that same ServiceProvider.
/// </summary>
internal sealed class WebViewServiceProvider : DisposableReactiveObjectWithLogger, IServiceProvider
{
    public WebViewServiceProvider()
    {
    }
    
    public IServiceProvider ServiceProvider { get; set; }

    public object GetService(Type serviceType)
    {
        var provider = ServiceProvider;
        if (provider == null)
        {
            throw new InvalidOperationException("Service provider is not ready");
        }

        return provider.GetService(serviceType);
    }
}