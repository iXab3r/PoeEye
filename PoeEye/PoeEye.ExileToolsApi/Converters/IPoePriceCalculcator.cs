using PoeShared.Common;

namespace PoeEye.ExileToolsApi.Converters
{
    internal interface IPoePriceCalculcator
    {
        PoePrice GetEquivalentInChaosOrbs(PoePrice price);
    }
}