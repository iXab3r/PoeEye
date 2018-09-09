using PoeShared.PoeTrade;
using Unity;
using Unity.Extension;

namespace PoeEye.PoeTradeRealtimeApi.Prism
{
    internal sealed class LiveRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterType<IRealtimeItemSource, RealtimeItemSource>();

            Container
                .RegisterSingleton<IPoeApi, PoeTradeRealtimeApi>(typeof(PoeTradeRealtimeApi).FullName);
        }
    }
}