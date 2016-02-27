namespace PoeEye.Prism
{
    using System.Collections.Specialized;

    using Communications;

    using Microsoft.Practices.Unity;

    using PoeShared.Http;
    using PoeShared.PoeTrade;
    using PoeShared.PoeTrade.Query;
    using PoeShared.Scaffolding;

    using PoeTrade;

    using ProxyProvider;

    using TypeConverter;

    public sealed class LiveRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterType<IHttpClient, GenericHttpClient>();

            Container
                .RegisterSingleton<IPoeApi, PoeTradeApi>()
                .RegisterSingleton<IPoeItemVerifier, PoeItemVerifier>()
                .RegisterSingleton<IConverter<NameValueCollection, string>, NameValueCollectionToQueryStringConverter>()
                .RegisterSingleton<IConverter<IPoeQueryInfo, IPoeQuery>, PoeQueryInfoToQueryConverter>()
                .RegisterSingleton<IConverter<IPoeQuery, NameValueCollection>, PoeQueryConverter>()
                .RegisterType<IPoeLiveHistoryProvider, PoeLiveHistoryProvider>();

            Container
                .RegisterSingleton<IProxyProvider, GenericProxyProvider>(new InjectionFactory(unity => new GenericProxyProvider()))
                .RegisterSingleton<IPoeTradeParser, PoeTradeParserModern>()
                .RegisterSingleton<IPoeQueryInfoProvider, PoeQueryInfoProvider>();
        }
    }
}