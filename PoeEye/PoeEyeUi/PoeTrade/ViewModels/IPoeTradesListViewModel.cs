namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;

    using PoeShared.Common;
    using PoeShared.PoeTrade;

    using ReactiveUI;

    internal interface IPoeTradesListViewModel
    {
        IPoeQueryInfo ActiveQuery { get; set; }

        ReactiveList<IPoeItem> HistoricalTrades { get; }

        bool IsBusy { get; }

        Exception LastUpdateException { get; }

        TimeSpan RecheckTimeout { get; set; }

        TimeSpan TimeSinceLastUpdate { get; }

        ReactiveList<IPoeTradeViewModel> TradesList { get; }
    }
}