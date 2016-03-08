namespace PoeEye.PoeTrade.Models
{
    using JetBrains.Annotations;

    internal interface IPoePriceCalculcator
    {
        float? GetEquivalentInChaosOrbs([CanBeNull] string rawPrice);
    }
}