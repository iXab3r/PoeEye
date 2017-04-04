using Microsoft.Practices.Unity;
using PoeShared.PoeTrade;
using PoeShared.Scaffolding;

namespace PoeEye.PoeTradeRealtimeApi.Prism
{
    internal sealed class LiveRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterType<IRealtimeItemSource, WebSocketRealtimeItemSource>();

            Container
                .RegisterSingleton<IPoeApi, PoeTradeRealtimeApi>(typeof(PoeTradeRealtimeApi).FullName);
        }
    }
}