using System;
using System.Collections.ObjectModel;
using JetBrains.Annotations;
using PoeShared.PoeTrade;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;

namespace PoeEye.PoeTrade.ViewModels
{
    internal interface IPoeTradesListViewModel : IDisposableReactiveObject
    {
        IPoeQueryInfo ActiveQuery { get; set; }

        TimeSpan RecheckPeriod { get; set; }

        bool IsBusy { get; }

        string Errors { get; }

        TimeSpan TimeSinceLastUpdate { get; }

        ReadOnlyObservableCollection<IPoeTradeViewModel> Items { [NotNull] get; }

        ReadOnlyObservableCollection<IPoeTradeViewModel> ItemsView { [NotNull] get; }

        IPageParameterDataViewModel PageParameters { [NotNull] get; }

        string QuickFilter { get; set; }

        void Refresh();

        void Clear();
    }
}