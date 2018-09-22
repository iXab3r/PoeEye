using PoeEye.PathOfExileTrade.TradeApi;
using PoeEye.PathOfExileTrade.TradeApi.Domain;
using PoeShared.PoeTrade;
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
                .RegisterType<IPathOfExileTradePortalApiLimiter, PathOfExileTradePortalApiLimiter>()
                .RegisterType<IPathOfExileTradeLiveAdapter, PathOfExileTradeLiveAdapter>();

            Container
                .RegisterSingleton<IConverter<IPoeQueryInfo, JsonSearchRequest.Query>, PoeQueryInfoToSearchRequestConverter>()
                .RegisterSingleton<IPoeApi, PathOfExileTradeApi>(typeof(PathOfExileTradeApi).FullName);
        }
    }
}