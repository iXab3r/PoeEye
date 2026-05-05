using Microsoft.Extensions.DependencyInjection;
using PoeShared.Blazor.Controls.GoldenLayout;
using PoeShared.Blazor.Controls.Services;

namespace PoeShared.Blazor.Controls.Prism;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddPoeSharedBlazorControls(this IServiceCollection services)
    {
        services.AddScoped<IScopedAntContainerRegistry, ScopedAntContainerRegistry>();
        services.AddScoped<IReactiveCollectionItemRegistry, ReactiveCollectionItemRegistry>();
        services.AddTransient<IGoldenLayoutBlazorAdapter, GLBlazorBlazorAdapter>();
        services.AddScoped<IGoldenLayoutInterop, GoldenLayoutInterop>();
        services.AddSingleton<IDynamicComponentParameterStorage, DynamicComponentParameterStorage>();
        return services;
    }
}
