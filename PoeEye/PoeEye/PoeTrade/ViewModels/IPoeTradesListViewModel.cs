using System.Collections.ObjectModel;
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

        bool IsBusy { get; }

        string Errors { get; }

        TimeSpan TimeSinceLastUpdate { get; }

        ReadOnlyObservableCollection<IPoeTradeViewModel> Items { [NotNull] get; }
        
        string QuickFilter { get; set; }

        void Refresh();

        void Clear();
    }
}