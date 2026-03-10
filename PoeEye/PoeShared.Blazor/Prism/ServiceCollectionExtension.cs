using System.Reactive.PlatformServices;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using PoeShared.Blazor.Scaffolding;
using PoeShared.Blazor.Services;
using Unity;

namespace PoeShared.Blazor.Prism;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddBlazorRepository(this IServiceCollection services, IUnityContainer unityContainer)
    {
        var viewRepository = unityContainer.Resolve<BlazorViewRepository>();
        var viewRepositoryContract = unityContainer.Resolve<IBlazorViewRepository>();
        var viewRegistrator = unityContainer.Resolve<IBlazorViewRegistrator>();

        services.AddSingleton(viewRepository);
        services.AddSingleton(viewRepositoryContract);
        services.AddSingleton(viewRegistrator);
        return services;
    }
    
    public static IServiceCollection AddBlazorContentRepository(this IServiceCollection services, IUnityContainer unityContainer)
    {
        var contentRepository = unityContainer.Resolve<BlazorContentRepository>();
        var contentRepositoryContract = unityContainer.Resolve<IBlazorContentRepository>();

        services.AddSingleton(contentRepository);
        services.AddSingleton(contentRepositoryContract);
        return services;
    }
    
    public static IServiceCollection AddBlazorUtils(this IServiceCollection services, IUnityContainer unityContainer)
    {
        services.AddScoped<IJsPoeBlazorUtils, JsPoeBlazorUtils>();
        services.AddSingleton(unityContainer.Resolve<ISystemClock>());

        return services;
    }
}
