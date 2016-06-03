using System.Collections.Specialized;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using Nest;
using PoeEye.ExileToolsApi.Converters;
using PoeEye.ExileToolsApi.Entities;
using PoeShared.Common;
using PoeShared.PoeTrade;
using PoeShared.PoeTrade.Query;
using PoeShared.Scaffolding;
using TypeConverter;

namespace PoeEye.ExileToolsApi.Prism
{
    internal sealed class LiveRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterSingleton<IPoePriceCalculcator, PriceToChaosCalculator>()
                .RegisterSingleton<IConverter<ItemConversionInfo, IPoeItem>, ToPoeItemConverter>()
                .RegisterSingleton<IConverter<IPoeQueryInfo, ISearchRequest>, PoeQueryInfoToSearchRequestConverter>();
            

            Container
                .RegisterSingleton<IPoeApi, ExileToolsApi>(typeof(ExileToolsApi).FullName);
        }

        private class MockedPoeItemVerifier : IPoeItemVerifier
        {
            public Task<bool?> Verify(IPoeItem item)
            {
                return new Task<bool?>(() => false);
            }
        }
    }
}