using System.IO.Abstractions;
using App.Metrics;
using Microsoft.Extensions.Logging;
using PoeShared.Caching;
using PoeShared.Evaluators;
using PoeShared.Modularity;
using PoeShared.Services;
using Unity;
using Unity.Extension;
using Unity.Lifetime;

namespace PoeShared.Prism;

public sealed class CommonRegistrations : UnityContainerExtension
{
    private static readonly IFluentLog Log = typeof(CommonRegistrations).PrepareLogger();

    protected override void Initialize()
    {
        Container
            .RegisterSingleton<IClock, Clock>()
            .RegisterSingleton<IComparisonService, ComparisonService>()
            .RegisterSingleton<IConfigSerializer, JsonConfigSerializer>()
            .RegisterSingleton(typeof(IConfigProvider<>), typeof(GenericConfigProvider<>))
            .RegisterSingleton<IAppArguments, AppArguments>()
            .RegisterSingleton<IRandomNumberGenerator, RandomNumberGenerator>()
            .RegisterSingleton<IAssemblyTracker, AssemblyTracker>()
            .RegisterSingleton<IPoeConfigConverterMigrationService, PoeConfigConverterMigrationService>()
            .RegisterSingleton<PoeConfigMetadataReplacementService>(typeof(IPoeConfigMetadataReplacementRepository), typeof(IPoeConfigMetadataReplacementService))
            .RegisterSingleton<IUniqueIdGenerator, UniqueIdGenerator>()
            .RegisterSingleton<ILoggerFactory, Log4NetLoggerFactory>()
            .RegisterSingleton<ILoggerProvider, Log4NetLoggerProvider>()
            .RegisterSingleton<IFileSystem, FileSystem>()
            .RegisterFactory<IMemoryPool>(x => MemoryPool.Shared, new ContainerControlledLifetimeManager());
            
        Container
            .RegisterSingleton<IMetricsRoot>(x =>
            {
                var appArguments = x.Resolve<IAppArguments>();
                MetricsService.Instance.Initialize(appArguments);
                return MetricsService.Instance.Metrics;
            })
            .RegisterSingleton<IMetrics>(x => x.Resolve<IMetricsRoot>());

        Container
            .RegisterType(typeof(IFactory<,,,>), typeof(Factory<,,,>))
            .RegisterType(typeof(IFactory<,,>), typeof(Factory<,,>))
            .RegisterType(typeof(IFactory<,>), typeof(Factory<,>))
            .RegisterType(typeof(IFactory<>),  typeof(Factory<>))
            .RegisterType(typeof(INamedFactory<>), typeof(Factory<,,,>))
            .RegisterType(typeof(INamedFactory<,,>), typeof(Factory<,,>))
            .RegisterType(typeof(INamedFactory<,>), typeof(Factory<,>))
            .RegisterType(typeof(INamedFactory<>),  typeof(Factory<>))
            .RegisterType(typeof(IMemoryCache<,>),  typeof(NaiveMemoryCache<,>))
            .RegisterType<IFolderCleanerService, FolderCleanerService>()
            .RegisterType<ISharedResourceLatch, SharedResourceLatch>();
        
        Container.RegisterCachingProxyFactory();
    }
}