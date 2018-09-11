using System.Collections.Specialized;
using PoeEye.PathOfExileTrade.TradeApi;
using PoeEye.PathOfExileTrade.TradeApi.Domain;
using PoeShared.PoeTrade;
using PoeShared.PoeTrade.Query;
using TypeConverter;
using Unity;
using Unity.Extension;

namespace PoeEye.PathOfExileTrade.Prism
{
    internal sealed class LiveRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterSingleton<IConverter<IPoeQueryInfo, JsonSearchRequest.Query>, PoeQueryInfoToSearchRequestConverter>()
                .RegisterSingleton<IPoeApi, PathOfExileTradeApi>(typeof(PathOfExileTradeApi).FullName);
        }
    }
}