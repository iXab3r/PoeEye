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
        services.AddScoped(typeof(ICachingProxyFactory<>), typeof(CachingProxyFactory<>));
    }
    
    public static void AddCommonService(this IServiceCollection services, IUnityContainer container)
    {
        services.AddSingleton(sp => container.Resolve<IAppArguments>());
        services.AddSingleton(sp => container.Resolve<IRandomNumberGenerator>());
        services.AddSingleton(sp => container.Resolve<IUniqueIdGenerator>());
        services.AddSingleton(sp => container.Resolve<IClock>());
        services.AddSingleton(sp => container.Resolve<IConfigSerializer>());
        services.AddSingleton(sp => container.Resolve<IFileDownloader>());
        services.AddSingleton(sp => container.Resolve<IAssemblyTracker>());
        services.AddSingleton(sp => container.Resolve<ISleepController>());
        services.AddSingleton(sp => container.Resolve<IMemoryPool>());
        services.AddSingleton(sp => container.Resolve<IComparisonService>());
        
        services.AddTransient(sp => container.Resolve<IFolderCleanerService>());
        services.AddTransient(sp => container.Resolve<IBufferedItemsProcessor>());
        services.AddTransient(sp => container.Resolve<ISharedResourceLatch>());
        
        services.AddSingleton(container);
        services.AddFactories();
    }
}