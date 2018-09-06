using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace PoeEye.PoeTrade.ViewModels
{
    internal interface IPoeAdvancedTradesListViewModel : IDisposableReactiveObject
    {
        void Add([NotNull] ReadOnlyObservableCollection<IPoeTradeViewModel> itemList);
        
        void Add([NotNull] ReadOnlyObservableCollection<IMainWindowTabViewModel> tabsList);
        
        ReadOnlyObservableCollection<IPoeTradeViewModel> Items { get; }
        
        void ResetSorting();
        
        void Filter(IObservable<Predicate<IPoeTradeViewModel>> conditionSource);
        
        void SortBy([NotNull] string propertyName, ListSortDirection direction);
        
        void ThenSortBy([NotNull] string propertyName, ListSortDirection direction);
    }
}