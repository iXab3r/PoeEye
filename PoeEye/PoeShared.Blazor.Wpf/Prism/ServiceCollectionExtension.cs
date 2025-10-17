using System.Reactive.PlatformServices;
using Microsoft.Extensions.DependencyInjection;
using PoeShared.Blazor.Services;
using PoeShared.Blazor.Wpf.Services;
using Unity;

namespace PoeShared.Blazor.Wpf.Prism;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddWebView2ContextMenuService(this IServiceCollection services, IUnityContainer unityContainer)
    {
        services.AddScoped<IBlazorContextMenuService, WebView2ContextMenuService>();
        return services;
    }  
    
    public static IServiceCollection AddWpfContextMenuService(this IServiceCollection services, IUnityContainer unityContainer)
    {
        services.AddScoped<IBlazorContextMenuService, WpfContextMenuService>();
        return services;
    }
}