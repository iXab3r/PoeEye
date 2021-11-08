using PoeShared.Modularity;
using PoeShared.Services;
using Unity;
using Unity.Extension;
using Unity.Lifetime;

namespace PoeShared.Prism
{
    public sealed class CommonRegistrations : UnityContainerExtension
    {
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
                .RegisterType(typeof(IFactory<,,,>), typeof(Factory<,,,>))
                .RegisterType(typeof(IFactory<,,>), typeof(Factory<,,>))
                .RegisterType(typeof(IFactory<,>), typeof(Factory<,>))
                .RegisterType<IFolderCleanerService, FolderCleanerService>()
                .RegisterType<ISharedResourceLatch, SharedResourceLatch>()
                .RegisterType(typeof(IFactory<>), typeof(Factory<>));
        }
    }
}