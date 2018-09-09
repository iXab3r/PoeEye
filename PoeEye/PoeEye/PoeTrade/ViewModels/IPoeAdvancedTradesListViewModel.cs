using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace PoeEye.PoeTrade.ViewModels
{
    internal interface IPoeAdvancedTradesListViewModel : IDisposableReactiveObject
    {
        ReadOnlyObservableCollection<IPoeTradeViewModel> Items { get; }

        int MaxItems { get; set; }
        void Add([NotNull] ReadOnlyObservableCollection<IPoeTradeViewModel> itemList);

        void Add([NotNull] ReadOnlyObservableCollection<IMainWindowTabViewModel> tabsList);

        void ResetSorting();

        void Filter(IObservable<Predicate<IPoeTradeViewModel>> conditionSource);

        void SortBy([NotNull] string propertyName, ListSortDirection direction);

        void ThenSortBy([NotNull] string propertyName, ListSortDirection direction);
    }
}