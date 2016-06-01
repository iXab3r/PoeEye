using PoeShared.Common;

namespace PoeEye.PoeTrade.Models
{
    internal interface IPoePriceCalculcator
    {
        PoePrice GetEquivalentInChaosOrbs(PoePrice price);
    }
}