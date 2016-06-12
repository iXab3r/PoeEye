using Microsoft.Practices.Unity;
using Nest;
using PoeEye.ExileToolsApi.Converters;
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
                .RegisterSingleton<ExileToolsSource, ExileToolsSource>()
                .RegisterSingleton<IPoePriceCalculcator, PriceToChaosCalculator>()
                .RegisterSingleton<IConverter<ItemConversionInfo, IPoeItem>, ToPoeItemConverter>()
                .RegisterSingleton<IConverter<IPoeQueryInfo, ISearchRequest>, PoeQueryInfoToSearchRequestConverter>();

            Container
                .RegisterSingleton<IPoeApi, ExileToolsApi>(typeof(ExileToolsApi).FullName);
        }
    }
}