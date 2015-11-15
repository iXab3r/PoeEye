namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;

    using JetBrains.Annotations;

    using PoeShared.PoeTrade;

    using ReactiveUI;

    internal interface IPoeTradesListViewModel
    {
        IPoeQueryInfo ActiveQuery { get; set; }

        IHistoricalTradesViewModel HistoricalTradesViewModel { [NotNull] get; }

        bool IsBusy { get; }

        Exception LastUpdateException { get; }

        TimeSpan RecheckTimeout { get; set; }

        TimeSpan TimeSinceLastUpdate { get; }

        IReactiveList<IPoeTradeViewModel> TradesList { [NotNull] get; }
    }
}