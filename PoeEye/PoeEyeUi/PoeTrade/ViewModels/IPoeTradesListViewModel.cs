namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;

    using JetBrains.Annotations;

    using PoeShared.PoeTrade;

    using ReactiveUI;

    internal interface IPoeTradesListViewModel
    {
        IPoeQueryInfo ActiveQuery { get; set; }

        TimeSpan RecheckPeriod { get; set; }

        IHistoricalTradesViewModel HistoricalTradesViewModel { [NotNull] get; }

        bool IsBusy { get; }

        string Errors { get; }

        TimeSpan TimeSinceLastUpdate { get; }

        IReactiveList<IPoeTradeViewModel> TradesList { [NotNull] get; }

        void Refresh();
    }
}