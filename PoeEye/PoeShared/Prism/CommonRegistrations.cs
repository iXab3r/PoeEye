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

namespace PoeShared.Prism
{
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
                .RegisterSingleton<IUniqueIdGenerator, UniqueIdGenerator>()
                .RegisterFactory<IMemoryPool>(x => MemoryPool.Shared, new ContainerControlledLifetimeManager());

            Container
                .RegisterSingleton<IMetricsRoot>(InitializeMetrics)
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

        private static IMetricsRoot InitializeMetrics(IUnityContainer container)
        {
            Log.Info("Initializing metrics...");
            var appArguments = container.Resolve<IAppArguments>();

            var metricsOutput = Path.Combine(appArguments.AppDataDirectory, $"logs", $"metrics{(appArguments.IsDebugMode ? "DebugMode" : default)}.txt");
            Log.Info($"Exporting metrics to file {metricsOutput}");
            var metrics = new MetricsBuilder()
                .Configuration.Configure(
                    options =>
                    {
                        options.Enabled = true;
                        options.ReportingEnabled = true;
                    })
                .Report.ToTextFile(options =>
                {
                    options.OutputPathAndFileName = metricsOutput;
                    options.AppendMetricsToTextFile = false;
                })
                .Build();
            return metrics;
        }
    }
}