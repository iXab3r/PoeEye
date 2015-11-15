namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;

    using Factory;

    using Guards;

    using JetBrains.Annotations;

    using Models;

    using PoeShared.Common;
    using PoeShared.Utilities;

    using ReactiveUI;

    internal sealed class HistoricalTradesViewModel : DisposableReactiveObject, IHistoricalTradesViewModel
    {
        private readonly IPoePriceCalculcator poePriceCalculcator;

        private bool isExpanded;

        public HistoricalTradesViewModel(
            [NotNull] IReactiveList<IPoeItem> historicalTrades,
            [NotNull] IReactiveList<IPoeTradeViewModel> actualTrades,
            [NotNull] IPoePriceCalculcator poePriceCalculcator)
        {
            Guard.ArgumentNotNull(() => actualTrades);
            Guard.ArgumentNotNull(() => historicalTrades);
            Guard.ArgumentNotNull(() => poePriceCalculcator);

            this.poePriceCalculcator = poePriceCalculcator;

            HistoricalTrades = historicalTrades;
        }

        public bool IsExpanded
        {
            get { return isExpanded; }
            set { this.RaiseAndSetIfChanged(ref isExpanded, value); }
        }

        public IReactiveList<IPoeItem> HistoricalTrades { get; } 
    }
}