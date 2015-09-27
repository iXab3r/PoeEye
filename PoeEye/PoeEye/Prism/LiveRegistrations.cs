namespace PoeEye.Prism
{
    using System.Collections.Specialized;

    using Communications;

    using Microsoft.Practices.Unity;

    using PoeShared;
    using PoeShared.Http;
    using PoeShared.PoeTrade;
    using PoeShared.PoeTrade.Query;

    using PoeTrade;
    using PoeTrade.Query;

    using TypeConverter;

    public sealed class LiveRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterType<IHttpClient, GenericHttpClient>();
            
            Container
                .RegisterType<IPoeApi, PoeTradeApi>()
                .RegisterType<IConverter<NameValueCollection, string>, NameValueCollectionToStringConverter>()
                .RegisterType<IConverter<IPoeQuery, NameValueCollection>, PoeQueryConverter>()
                .RegisterType<IPoeLiveHistoryProvider, PoeLiveHistoryProvider>()
                .RegisterType<IPoeQueryInfoProvider, PoeQueryInfoProvider>()
                .RegisterType<IClock, Clock>()
                .RegisterType<IPoeTradeParser, PoeTradeParserModern>();
        }
    }
}