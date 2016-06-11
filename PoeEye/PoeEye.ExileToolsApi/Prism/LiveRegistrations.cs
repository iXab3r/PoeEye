using Microsoft.Practices.Unity;
using Nest;
using PoeEye.ExileToolsApi.Converters;
using PoeEye.ExileToolsApi.RealtimeApi;
using PoeEye.ExileToolsApi.RealtimeApi.Entities;
using PoeShared.Common;
using PoeShared.PoeTrade;
using PoeShared.Scaffolding;
using TypeConverter;

namespace PoeEye.ExileToolsApi.Prism
{
    internal sealed class LiveRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterType<IRealtimeItemSource, RealtimeItemSource>();

            Container
                .RegisterSingleton<ExileToolsSource, ExileToolsSource>()
                .RegisterSingleton<IPoePriceCalculcator, PriceToChaosCalculator>()
                .RegisterSingleton<IConverter<ItemConversionInfo, IPoeItem>, ToPoeItemConverter>()
                .RegisterSingleton<IConverter<IPoeQueryInfo, RealtimeQuery>, PoeQueryInfoToRealtimeSearchRequestConverter>()
                .RegisterSingleton<IConverter<IPoeQueryInfo, ISearchRequest>, PoeQueryInfoToSearchRequestConverter>();

            Container
                .RegisterSingleton<IPoeApi, ExileToolsApi>(typeof(ExileToolsApi).FullName)
                .RegisterSingleton<IPoeApi, ExileToolsRealtimeApi>(typeof(ExileToolsRealtimeApi).FullName);
        }
    }
}