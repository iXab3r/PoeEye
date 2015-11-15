namespace PoeEyeUi.PoeTrade.ViewModels
{
    using PoeShared.Common;

    using ReactiveUI;

    internal interface IHistoricalTradesViewModel
    {
        bool IsExpanded { get; }

        IReactiveList<IPoeTradeViewModel> ItemsViewModels { get; }

        void AddItems(params IPoeItem[] items);

        void Clear();
    }
}