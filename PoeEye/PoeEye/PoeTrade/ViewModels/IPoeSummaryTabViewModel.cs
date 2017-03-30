using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;
using JetBrains.Annotations;
using PoeEye.PoeTrade.Common;
using ReactiveUI;

namespace PoeEye.PoeTrade.ViewModels
{
    internal interface IPoeSummaryTabViewModel
    {
        IEnumerable<PoeFilteredTradeViewModel> TradesView { get; }

        ICommand MarkAllAsReadCommand { [NotNull] get; }

        bool ShowNewItems { get; set; }

        bool ShowRemovedItems { get; set; }

        SortDescriptionData[] SortingOptions { [NotNull] get; }

        SortDescriptionData ActiveSortDescriptionData { [CanBeNull] get; [CanBeNull] set; }
    }
}