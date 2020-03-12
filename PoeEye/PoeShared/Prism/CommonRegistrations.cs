using PoeShared.Modularity;
using PoeShared.Services;
using Unity;
using Unity.Extension;

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
                .RegisterFactory<IAppArguments>(container => AppArguments.Instance)
                .RegisterSingleton<IRandomNumberGenerator, RandomNumberGenerator>();

            Container
                .RegisterType(typeof(IFactory<,,,>), typeof(Factory<,,,>))
                .RegisterType(typeof(IFactory<,,>), typeof(Factory<,,>))
                .RegisterType(typeof(IFactory<,>), typeof(Factory<,>))
                .RegisterType(typeof(IFactory<>), typeof(Factory<>));
        }
    }
}