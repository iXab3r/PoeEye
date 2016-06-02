using PoeShared.Communications;

namespace PoeEye.PoeTrade.Prism
{
    using System.Collections.Specialized;
    using Microsoft.Practices.Unity;
    using PoeShared.PoeTrade;
    using PoeShared.PoeTrade.Query;
    using PoeShared.Scaffolding;

    using ProxyProvider;

    using TypeConverter;

    internal sealed class LiveRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        { 
            Container
                .RegisterSingleton<IPoeApi, PoeTradeApi>(typeof(PoeTradeApi).FullName)
                .RegisterSingleton<IConverter<IPoeQueryInfo, IPoeQuery>, PoeQueryInfoToQueryConverter>()
                .RegisterSingleton<IConverter<IPoeQuery, NameValueCollection>, PoeQueryConverter>()
                .RegisterSingleton<IPoeTradeDateTimeExtractor, PoeTradeDateTimeExtractor>();

            Container
                .RegisterSingleton<IPoeTradeParser, PoeTradeParserModern>();
        }
    }
}