using Microsoft.Practices.Unity;
using PoeEye.ExileToolsApi.RealtimeApi;
using PoeEye.ExileToolsApi.RealtimeApi.Entities;
using PoeEye.ExileToolsRealtimeApi.Converters;
using PoeEye.ExileToolsRealtimeApi.RealtimeApi;
using PoeShared.PoeTrade;
using PoeShared.Scaffolding;
using TypeConverter;

namespace PoeEye.ExileToolsRealtimeApi.Prism
{
    internal sealed class LiveRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterType<IConverter<IPoeQueryInfo, RealtimeQuery>, PoeQueryInfoToRealtimeSearchRequestConverter>()
                .RegisterType<IRealtimeItemSource, RealtimeItemSource>();

            Container
                .RegisterSingleton<IConverter<IPoeQueryInfo, RealtimeQuery>, PoeQueryInfoToRealtimeSearchRequestConverter>();

            Container
                .RegisterSingleton<IPoeApi, ExileToolsRealtimeApi>(typeof(ExileToolsRealtimeApi).FullName);
        }
    }
}