namespace PoeEye.Prism
{
    using System.Collections.Specialized;
    using System.Net;

    using Communications;

    using Microsoft.Practices.Unity;

    using PoeShared;
    using PoeShared.Http;
    using PoeShared.PoeTrade;
    using PoeShared.PoeTrade.Query;

    using PoeTrade;
    using PoeTrade.Query;

    using ProxyProvider;

    using TypeConverter;

    public sealed class LiveRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterType<IHttpClient, GenericHttpClient>();

            Container
                .RegisterType<IPoeApi, PoeTradeApi>(new ContainerControlledLifetimeManager())
                .RegisterType<IPoeItemVerifier, PoeItemVerifier>(new ContainerControlledLifetimeManager())
                .RegisterType<IConverter<NameValueCollection, string>, NameValueCollectionToQueryStringConverter>(new ContainerControlledLifetimeManager())
                .RegisterType<IConverter<IPoeQueryInfo, IPoeQuery>, PoeQueryInfoToQueryConverter>(new ContainerControlledLifetimeManager())
                .RegisterType<IConverter<IPoeQuery, NameValueCollection>, PoeQueryConverter>(new ContainerControlledLifetimeManager())
                .RegisterType<IPoeLiveHistoryProvider, PoeLiveHistoryProvider>();

            Container
                .RegisterType<IClock, Clock>(new ContainerControlledLifetimeManager())
                .RegisterType<IProxyProvider, GenericProxyProvider>(new ContainerControlledLifetimeManager(), new InjectionFactory(unity => new GenericProxyProvider()))
                .RegisterType<IPoeTradeParser, PoeTradeParserModern>(new ContainerControlledLifetimeManager())
                .RegisterType<IPoeQueryInfoProvider, PoeQueryInfoProvider>(new ContainerControlledLifetimeManager());
        }
    }
}