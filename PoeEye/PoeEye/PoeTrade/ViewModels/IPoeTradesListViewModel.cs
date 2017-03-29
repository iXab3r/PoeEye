﻿using System.Collections.ObjectModel;
using DynamicData;
using PoeShared.Scaffolding;

namespace PoeEye.PoeTrade.ViewModels
{
    using System;

    using JetBrains.Annotations;

    using PoeShared.PoeTrade;

    using ReactiveUI;

    internal interface IPoeTradesListViewModel : IDisposableReactiveObject
    {
        IPoeQueryInfo ActiveQuery { get; set; }

        TimeSpan RecheckPeriod { get; set; }

        IHistoricalTradesViewModel HistoricalTrades { [NotNull] get; }

        bool IsBusy { get; }

        string Errors { get; }

        TimeSpan TimeSinceLastUpdate { get; }

        ReadOnlyObservableCollection<IPoeTradeViewModel> Items { [NotNull] get; }

        void Refresh();

        void Clear();
    }
}