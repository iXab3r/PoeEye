using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using JetBrains.Annotations;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;

namespace PoeEye.PoeTrade.ViewModels
{
    internal interface IPoeAdvancedTradesListViewModel : IDisposableReactiveObject
    {
        /// <summary>
        ///   Filtered and sorted items list
        /// </summary>
        ReadOnlyObservableCollection<IPoeTradeViewModel> Items { get; }
        
        /// <summary>
        ///  Raw items list - without sorting/paging/filtering/etc
        /// </summary>
        ReadOnlyObservableCollection<IPoeTradeViewModel> RawItems { get; }
        
        IPageParameterDataViewModel PageParameter { [NotNull] get; }

        void Add([NotNull] ReadOnlyObservableCollection<IPoeTradeViewModel> itemList);

        void Add([NotNull] ReadOnlyObservableCollection<IMainWindowTabViewModel> tabsList);

        void ResetSorting();

        void Filter(IObservable<Predicate<IPoeTradeViewModel>> conditionSource);

        void SortBy([NotNull] string propertyName, ListSortDirection direction);

        void ThenSortBy([NotNull] string propertyName, ListSortDirection direction);
    }
}