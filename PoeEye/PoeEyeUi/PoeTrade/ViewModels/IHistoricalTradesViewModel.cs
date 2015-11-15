namespace PoeEyeUi.PoeTrade.ViewModels
{
    using PoeShared.Common;

    using ReactiveUI;

    internal interface IHistoricalTradesViewModel
    {
        bool IsExpanded { get; }

        IReactiveList<IPoeItem> HistoricalTrades { get; } 
    }
}