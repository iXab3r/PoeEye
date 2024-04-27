using Microsoft.Extensions.DependencyInjection;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Services;
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
        services.AddSingleton(sp => container.Resolve<IUniqueIdGenerator>());
        services.AddSingleton(sp => container.Resolve<IClock>());
        services.AddSingleton(sp => container.Resolve<IConfigSerializer>());
        services.AddSingleton(sp => container.Resolve<IAppArguments>());
        
        services.AddSingleton(container);
        services.AddTransient(typeof(IFactory<>), typeof(Factory<>));
    }
}