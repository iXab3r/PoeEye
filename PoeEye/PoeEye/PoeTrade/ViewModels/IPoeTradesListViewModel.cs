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
        
        string RawQuery { [CanBeNull] get; }

        TimeSpan RecheckPeriod { get; set; }

        bool IsBusy { get; }

        string Errors { get; }

        TimeSpan TimeSinceLastUpdate { get; }

        IPoeAdvancedTradesListViewModel ItemList { [NotNull] get; }

        string QuickFilter { get; set; }

        void Refresh();

        void Clear();
    }
}