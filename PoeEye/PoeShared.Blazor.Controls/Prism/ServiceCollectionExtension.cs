using Microsoft.Extensions.DependencyInjection;
using PoeShared.Blazor.Controls.Services;

namespace PoeShared.Blazor.Controls.Prism;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddPoeSharedBlazorControls(this IServiceCollection services)
    {
        services.AddScoped<IScopedAntContainerRegistry, ScopedAntContainerRegistry>();
        services.AddScoped<IReactiveCollectionItemRegistry, ReactiveCollectionItemRegistry>();
        return services;
    }
}
