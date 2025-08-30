using System.Reactive.PlatformServices;
using Microsoft.Extensions.DependencyInjection;
using PoeShared.Blazor.Controls.Services;
using PoeShared.Blazor.Services;
using Unity;

namespace PoeShared.Blazor.Controls.Prism;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddBlazorControls(this IServiceCollection services, IUnityContainer unityContainer)
    {
        services.AddScoped<IScopedAntContainerRegistry, ScopedAntContainerRegistry>();
        return services;
    }
}