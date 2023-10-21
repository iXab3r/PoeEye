using Microsoft.Extensions.DependencyInjection;
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
}