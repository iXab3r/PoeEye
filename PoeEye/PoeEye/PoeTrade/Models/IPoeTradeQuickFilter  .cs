using JetBrains.Annotations;
using PoeEye.PoeTrade.ViewModels;

namespace PoeEye.PoeTrade.Models
{
    internal interface IPoeTradeQuickFilter
    {
        bool Apply([CanBeNull] string text, [CanBeNull] IPoeTradeViewModel trade);
    }
}