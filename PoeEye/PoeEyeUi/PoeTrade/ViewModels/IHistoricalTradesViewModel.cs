namespace PoeEyeUi.PoeTrade.ViewModels
{
    using JetBrains.Annotations;

    using Models;

    using ReactiveUI;

    internal interface IHistoricalTradesViewModel
    {
        IReactiveList<PoeItemPricePoint> HistoricalPoints { [NotNull] get; }

        IReactiveList<PoeItemPricePoint> ActualPoints { [NotNull] get; }

        bool IsExpanded { get; }
    }
}