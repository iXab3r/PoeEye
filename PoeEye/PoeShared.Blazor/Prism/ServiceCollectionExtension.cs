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
        services.AddSingleton<BlazorViewRepository>(provider => unityContainer.Resolve<BlazorViewRepository>());
        services.AddSingleton<IBlazorViewRepository>(provider => unityContainer.Resolve<IBlazorViewRepository>());
        services.AddSingleton<IBlazorViewRegistrator>(provider => unityContainer.Resolve<IBlazorViewRegistrator>());
        return services;
    }
    
    public static IServiceCollection AddBlazorContentRepository(this IServiceCollection services, IUnityContainer unityContainer)
    {
        services.AddSingleton<BlazorContentRepository>(provider => unityContainer.Resolve<BlazorContentRepository>());
        services.AddSingleton<IBlazorContentRepository>(provider => unityContainer.Resolve<IBlazorContentRepository>());
        return services;
    }
    
    public static IServiceCollection AddBlazorUtils(this IServiceCollection services, IUnityContainer unityContainer)
    {
        services.AddScoped<IJsPoeBlazorUtils, JsPoeBlazorUtils>();
        services.AddSingleton<Microsoft.Extensions.Internal.ISystemClock>(provider => unityContainer.Resolve<Microsoft.Extensions.Internal.ISystemClock>());

        return services;
    }
}