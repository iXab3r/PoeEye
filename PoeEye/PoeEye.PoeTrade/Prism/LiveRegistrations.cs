using System.Collections.Specialized;
using PoeEye.PoeTrade.TradeApi;
using PoeShared.PoeTrade;
using PoeShared.PoeTrade.Query;
using TypeConverter;
using Unity;
using Unity.Extension;

namespace PoeEye.PoeTrade.Prism
{
    internal sealed class LiveRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterSingleton<IPoeApi, PoeTradeApi>(typeof(PoeTradeApi).FullName)
                .RegisterSingleton<IConverter<IPoeQueryInfo, IPoeQuery>, PoeQueryInfoToQueryConverter>()
                .RegisterSingleton<IConverter<IPoeQuery, NameValueCollection>, PoeQueryConverter>()
                .RegisterSingleton<IPoeTradeDateTimeExtractor, PoeTradeDateTimeExtractor>()
                .RegisterSingleton<IPoeTradeParser, PoeTradeParserModern>();

            Container
                .RegisterType<IPoeTradeLiveAdapter, RealtimeItemSource>();
        }
    }
}