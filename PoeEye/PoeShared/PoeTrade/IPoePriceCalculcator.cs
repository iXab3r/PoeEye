using PoeShared.Common;

namespace PoeShared.PoeTrade
{
    public interface IPoePriceCalculcator
    {
        PoePrice GetEquivalentInChaosOrbs(PoePrice price);
    }
}