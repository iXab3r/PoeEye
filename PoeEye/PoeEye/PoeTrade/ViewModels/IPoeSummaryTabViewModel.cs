using System.Collections.ObjectModel;
using System.Windows.Input;
using JetBrains.Annotations;
using PoeEye.Scaffolding;
using PoeShared.Scaffolding;

namespace PoeEye.PoeTrade.ViewModels
{
    internal interface IPoeSummaryTabViewModel : IDisposableReactiveObject
    {
        ReadOnlyObservableCollection<IPoeTradeViewModel> TradesView { get; }

        ICommand MarkAllAsReadCommand { [NotNull] get; }

        bool ShowNewItems { get; set; }

        bool ShowRemovedItems { get; set; }

        SortDescriptionData[] SortingOptions { [NotNull] get; }

        SortDescriptionData ActiveSortDescriptionData { [CanBeNull] get; [CanBeNull] set; }
    }
}