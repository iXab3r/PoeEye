using System.Collections.Specialized;
using System.Diagnostics;
using PoeShared.Communications;
using PoeShared.Modularity;
using PoeShared.PoeTrade.Query;
using ProxyProvider;

namespace PoeShared.Prism
{
    using System.Collections.Generic;

    using Common;

    using Microsoft.Practices.Unity;

    using PoeDatabase;

    using PoeTrade;

    using Scaffolding;

    using TypeConverter;

    public sealed class CommonRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterSingleton<IClock, Clock>()
                .RegisterSingleton<IPoeEyeModulesRegistrator, PoeEyeModulesRegistrator>()
                .RegisterSingleton<IPoeEyeModulesEnumerator, PoeEyeModulesRegistrator>()
                .RegisterSingleton<IPoeDatabaseReader, StaticPoeDatabaseReader>()
                .RegisterSingleton<IEqualityComparer<IPoeItem>, PoeItemEqualityComparer>()
                .RegisterSingleton<IConverter<NameValueCollection, string>, NameValueCollectionToQueryStringConverter>()
                .RegisterSingleton<IProxyProvider, GenericProxyProvider>(new InjectionFactory(unity => new GenericProxyProvider()))
                .RegisterSingleton<IRandomNumberGenerator, RandomNumberGenerator>();

            Container
                .RegisterType<IPoeLiveHistoryProvider, PoeLiveHistoryProvider>()
                .RegisterType<IHttpClient, GenericHttpClient>()
                .RegisterType<IPoeApiWrapper, PoeApiWrapper>()
                .RegisterType(typeof (IFactory<,,>), typeof (Factory<,,>))
                .RegisterType(typeof (IFactory<,>), typeof (Factory<,>))
                .RegisterType(typeof (IFactory<>), typeof (Factory<>));
        }
    }
}