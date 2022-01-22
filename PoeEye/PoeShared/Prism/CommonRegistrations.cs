using System;
using System.IO;
using App.Metrics;
using PoeShared.Logging;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
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
            .RegisterSingleton<IPoeConfigConverterMigrationService, PoeConfigConverterMigrationService>()
            .RegisterSingleton<IPoeConfigMetadataReplacementService,PoeConfigMetadataReplacementService>()
            .RegisterSingleton<IUniqueIdGenerator, UniqueIdGenerator>()
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
            .RegisterType( typeof(IFactory<,,,>), typeof(Factory<,,,>))
            .RegisterType(typeof(IFactory<,,>), typeof(Factory<,,>))
            .RegisterType(typeof(IFactory<,>), typeof(Factory<,>))
            .RegisterType(typeof(IFactory<>),  typeof(Factory<>))
            .RegisterType(typeof(INamedFactory<>), typeof(Factory<,,,>))
            .RegisterType(typeof(INamedFactory<,,>), typeof(Factory<,,>))
            .RegisterType(typeof(INamedFactory<,>), typeof(Factory<,>))
            .RegisterType(typeof(INamedFactory<>),  typeof(Factory<>))
            .RegisterType<IFolderCleanerService, FolderCleanerService>()
            .RegisterType<ISharedResourceLatch, SharedResourceLatch>();
    }
}