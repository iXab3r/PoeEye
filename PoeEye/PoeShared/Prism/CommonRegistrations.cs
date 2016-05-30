﻿using System.Collections.Specialized;
using PoeShared.Communications;
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
                .RegisterSingleton<IPoeDatabaseReader, StaticPoeDatabaseReader>()
                .RegisterSingleton<IPoeItemParser, PoeItemParser>()
                .RegisterSingleton<IEqualityComparer<IPoeItem>, PoeItemEqualityComparer>()
                .RegisterSingleton<IConverter<IPoeItem, IPoeQueryInfo>, PoeItemToPoeQueryConverter>()
                .RegisterSingleton<IConverter<NameValueCollection, string>, NameValueCollectionToQueryStringConverter>()
                .RegisterSingleton<IProxyProvider, GenericProxyProvider>(new InjectionFactory(unity => new GenericProxyProvider()))
                .RegisterSingleton<IPoeStaticData, PoeQueryInfoProvider>()
                .RegisterSingleton<IRandomNumberGenerator, RandomNumberGenerator>();

            Container
                .RegisterType(typeof (IPoeModsProcessor), typeof (PoeModsProcessor))
                .RegisterType<IPoeLiveHistoryProvider, PoeLiveHistoryProvider>()
                .RegisterType<IHttpClient, GenericHttpClient>()
                .RegisterType(typeof (IFactory<,,>), typeof (Factory<,,>))
                .RegisterType(typeof (IFactory<,>), typeof (Factory<,>))
                .RegisterType(typeof (IFactory<>), typeof (Factory<>));
        }
    }
}