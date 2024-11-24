using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using PoeShared.Bindings;
using PoeShared.Caching;
using PoeShared.Modularity;
using PoeShared.Services;
using Unity;
using Unity.Extension;

namespace PoeShared.Prism;

public sealed class CommonRegistrations : UnityContainerExtension
{
    private static readonly IFluentLog Log = typeof(CommonRegistrations).PrepareLogger();

    protected override void Initialize()
    {
        Container
            .RegisterSingleton<IClock, Clock>()
            .RegisterSingleton<IConfigProviderFromFile, ConfigProviderFromFile>()
            .RegisterSingleton<IComparisonService, ComparisonService>()
            .RegisterSingleton<IConfigSerializer>(x =>
            {
                var serializer = x.Resolve<JsonConfigSerializer>();
                
                var configConverter = x.Resolve<PoeConfigConverter>();
                serializer.RegisterConverter(configConverter);

                var binaryDataConverter = x.Resolve<BinaryResourceRefConverter>();
                serializer.RegisterConverter(binaryDataConverter);
                
                return serializer;
            })
            .RegisterSingleton(typeof(IConfigProvider<>), typeof(GenericConfigProvider<>))
            .RegisterSingleton<IAppArguments, AppArguments>()
            .RegisterSingleton<IRandomNumberGenerator>(_ => RandomNumberGenerator.Instance)
            .RegisterSingleton<IPoeConfigConverterMigrationService, PoeConfigConverterMigrationService>()
            .RegisterSingleton<PoeConfigMetadataReplacementService>(typeof(IPoeConfigMetadataReplacementRepository), typeof(IPoeConfigMetadataReplacementService))
            .RegisterSingleton<IUniqueIdGenerator, UniqueIdGenerator>()
            .RegisterSingleton<ILoggerFactory, Log4NetLoggerFactory>()
            .RegisterSingleton<ILoggerProvider, Log4NetLoggerProvider>()
            .RegisterSingleton<IFileSystem, FileSystem>()
            .RegisterSingleton<IAssemblyTracker>(x => IAssemblyTracker.Instance)
            .RegisterSingleton<ISleepController>(x => SleepController.Instance)
            .RegisterSingleton<ICsharpExpressionParser>(x => CsharpExpressionParser.Instance)
            .RegisterSingleton<IMemoryPool>(x => MemoryPool.Shared);
            
        Container
            .RegisterType(typeof(IFactory<,,,,,>), typeof(Factory<,,,,,>))
            .RegisterType(typeof(IFactory<,,,,>), typeof(Factory<,,,,>))
            .RegisterType(typeof(IFactory<,,,>), typeof(Factory<,,,>))
            .RegisterType(typeof(IFactory<,,>), typeof(Factory<,,>))
            .RegisterType(typeof(IFactory<,>), typeof(Factory<,>))
            .RegisterType(typeof(IFactory<>),  typeof(Factory<>))
            .RegisterType(typeof(INamedFactory<>), typeof(Factory<,,,>))
            .RegisterType(typeof(INamedFactory<,,>), typeof(Factory<,,>))
            .RegisterType(typeof(INamedFactory<,>), typeof(Factory<,>))
            .RegisterType(typeof(INamedFactory<>),  typeof(Factory<>))
            .RegisterType(typeof(IMemoryCache<,>),  typeof(NaiveMemoryCache<,>))
            .RegisterType<IBufferedItemsProcessor, BufferedItemsProcessor>()
            .RegisterType<IFolderCleanerService, FolderCleanerService>()
            .RegisterType<ISharedResourceLatch, SharedResourceLatch>();
        
        Container.RegisterCachingProxyFactory();
    }
}