using Microsoft.Extensions.DependencyInjection;
using PoeShared.Prism;
using Unity;

namespace PoeShared.Caching;

public static class CachingRegistrations
{
    public static void RegisterCachingProxyFactory(this IUnityContainer container)
    {
        container
            .RegisterSingleton<CachingProxyFactory>(typeof(ICachingProxyFactory), typeof(ICachingProxyFactoryConfigurator))
            .RegisterSingleton(typeof(ICachingProxyFactory<>), typeof(CachingProxyFactory<>));
    }
    
    public static void AddCachingProxyFactory(this IServiceCollection services, IUnityContainer container)
    {
        services.AddSingleton(typeof(ICachingProxyFactory), _ => container.Resolve<ICachingProxyFactory>());
        services.AddSingleton(typeof(ICachingProxyFactoryConfigurator),  _ => container.Resolve<ICachingProxyFactory>());
        services.AddTransient(typeof(ICachingProxyFactory<>), typeof(CachingProxyFactory<>));
    }
    
    public static void AddCommonService(this IServiceCollection services, IUnityContainer container)
    {
        services.AddSingleton(container);
        services.AddTransient(typeof(IFactory<>), typeof(Factory<>));
    }
}