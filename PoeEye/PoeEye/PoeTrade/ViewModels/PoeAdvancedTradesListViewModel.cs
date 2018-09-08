using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using DynamicData;
using DynamicData.Binding;
using DynamicData.Controllers;
using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeEye.PoeTrade.Common;
using PoeShared;
using PoeShared.Common;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeEye.PoeTrade.ViewModels
{
    internal sealed class PoeAdvancedTradesListViewModel : DisposableReactiveObject, IPoeAdvancedTradesListViewModel
    {
        private readonly ReadOnlyObservableCollection<IPoeTradeViewModel> itemsCollection;
        private static readonly Func<IPoeTradeViewModel, bool> AlwaysTruePredicate = model => true;
        private static readonly TimeSpan ResortRefilterThrottleTimeout = TimeSpan.FromMilliseconds(100);

        private readonly ISubject<Unit> resortRequest = new Subject<Unit>();
        private readonly SourceList<ISourceList<IPoeTradeViewModel>> tradeLists = new SourceList<ISourceList<IPoeTradeViewModel>>();

        private readonly SourceCache<SortData, SortDescriptionData> sortingCache =
            new SourceCache<SortData, SortDescriptionData>(x => x.Data);

        private readonly SourceList<SortData> sortingRules = new SourceList<SortData>();

        private readonly BehaviorSubject<Func<IPoeTradeViewModel, bool>> filterConditionSource = new BehaviorSubject<Func<IPoeTradeViewModel, bool>>(null);
        private readonly SerialDisposable activeFilterAnchor = new SerialDisposable();

        private long filterRequestsCount = 0;
        private long sortRequestsCount = 0;
        private int maxItems = 0;

        public PoeAdvancedTradesListViewModel([NotNull] [Dependency(WellKnownSchedulers.UI)]
            IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));

            activeFilterAnchor.AddTo(Anchors);

            var comparerObservable = sortingRules
                .Connect()
                .ToUnit()
                .StartWith()
                .Select(x => new TradeComparer(sortingRules.Items.ToArray()));

            var allItems = tradeLists
                .Or();

            allItems
                .Filter(filterConditionSource.Select(x => x ?? AlwaysTruePredicate).Sample(ResortRefilterThrottleTimeout)
                    .Do(_ => Interlocked.Increment(ref filterRequestsCount)))
                .Virtualise(this.WhenAnyValue(x => x.MaxItems).Select(x => new VirtualRequest(0, x > 0 ? x : int.MaxValue)))
                .Sort(comparerObservable, SortOptions.None,
                    resortRequest.Sample(ResortRefilterThrottleTimeout).Do(_ => Interlocked.Increment(ref sortRequestsCount)))
                .ObserveOn(uiScheduler)
                .Bind(out itemsCollection)
                .Subscribe()
                .AddTo(Anchors);

            // Filtering / Sorting    
            RegisterSort(nameof(IPoeTradeViewModel.TradeState), x => x?.TradeState);
            RegisterSort(nameof(IPoeTradeViewModel.PriceInChaosOrbs), x => x?.PriceInChaosOrbs);
            RegisterSort(nameof(IPoeItem.Timestamp), x => x?.Trade?.Timestamp);

            allItems
                .WhenAnyPropertyChanged()
                .ToUnit()
                .Subscribe(resortRequest)
                .AddTo(Anchors);

            allItems
                .WhenAnyPropertyChanged()
                .Subscribe(() => filterConditionSource.OnNext(filterConditionSource.Value))
                .AddTo(Anchors);
        }

        public void Add(ReadOnlyObservableCollection<IPoeTradeViewModel> itemList)
        {
            Guard.ArgumentNotNull(itemList, nameof(itemList));

            var proxy = new ItemListProxy(itemList, resortRequest).AddTo(Anchors);
            tradeLists.Add(proxy.Items);
        }

        public void Add(ReadOnlyObservableCollection<IMainWindowTabViewModel> tabsList)
        {
            Guard.ArgumentNotNull(tabsList, nameof(tabsList));

            var srcListChangeSet =
                tabsList
                    .ToObservableChangeSet()
                    .Transform(tab => new TabProxy(tab, resortRequest))
                    .DisposeMany()
                    .Transform(x => x.Items)
                    .DisposeMany()
                    .ToSourceList();

            //FIXME Potential bug - maybe it would be better to check for a specific action(Add,Remove,Reset,Clear) rather than handling OnItemAdded/Removed
            srcListChangeSet.Connect().OnItemAdded(item => tradeLists.Add(item)).Subscribe().AddTo(Anchors);
            srcListChangeSet.Connect().OnItemRemoved(item => tradeLists.Remove(item)).Subscribe().AddTo(Anchors);
        }

        public ReadOnlyObservableCollection<IPoeTradeViewModel> Items => itemsCollection;
        
        
        public int MaxItems
        {
            get { return maxItems; }
            set { this.RaiseAndSetIfChanged(ref maxItems, value); }
        }
        
        public long FilterRequestsCount => filterRequestsCount;

        public long SortRequestsCount => sortRequestsCount;

        public void ResetSorting()
        {
            sortingRules.Clear();
        }

        public void Filter(IObservable<Predicate<IPoeTradeViewModel>> conditionSource)
        {
            conditionSource.Subscribe(condition => filterConditionSource.OnNext(new Func<IPoeTradeViewModel, bool>(condition))).AssignTo(activeFilterAnchor);
        }

        public void RegisterSort(string propertyName, Func<IPoeTradeViewModel, object> fieldExtractor)
        {
            var asc = new SortDescriptionData(propertyName, ListSortDirection.Ascending);
            var desc = new SortDescriptionData(propertyName, ListSortDirection.Descending);
            if (sortingCache.Lookup(asc).HasValue || sortingCache.Lookup(desc).HasValue)
            {
                throw new ApplicationException($"Cache already contains item with a key '{propertyName}', cache: ${sortingCache.Keys.DumpToTextRaw()}");
            }

            sortingCache.AddOrUpdate(new SortData(asc, fieldExtractor));
            sortingCache.AddOrUpdate(new SortData(desc, fieldExtractor));
        }

        public void SortBy(string propertyName, ListSortDirection direction)
        {
            sortingRules.Clear();
            ThenSortBy(propertyName, direction);
        }

        public void ThenSortBy(string propertyName, ListSortDirection direction)
        {
            var sort = new SortDescriptionData(propertyName, direction);
            var sortData = sortingCache.Lookup(sort);
            if (!sortData.HasValue)
            {
                throw new ApplicationException($"Could not find item with a key '{sort}', cache: ${sortingCache.Keys.DumpToTextRaw()}");
            }

            sortingRules.Add(sortData.Value);
        }

        private sealed class TabProxy : DisposableReactiveObject
        {
            public TabProxy(
                IMainWindowTabViewModel tab, ISubject<Unit> filterRequestSubject)
            {
                Guard.ArgumentNotNull(tab, nameof(tab));
                Guard.ArgumentNotNull(filterRequestSubject, nameof(filterRequestSubject));

                var listOfItemLists = new SourceList<ISourceList<IPoeTradeViewModel>>();

                var activeTradeListAnchors = new SerialDisposable().AddTo(Anchors);
                tab
                    .WhenAnyValue(x => x.TradesList)
                    .Select(x => x.Items)
                    .Subscribe(
                        items =>
                        {
                            var proxy = new ItemListProxy(items, filterRequestSubject).AssignTo(activeTradeListAnchors);
                            listOfItemLists.Clear();
                            listOfItemLists.Add(proxy.Items);
                        })
                    .AddTo(Anchors);

                Items = listOfItemLists.Or().ToSourceList();

                Disposable.Create(() => Log.Instance.Trace($"[PoeAdvancedTradesListViewModel.TabProxy] Proxy for tab {tab} ({tab.TabName}) was disposed"))
                    .AddTo(Anchors);
            }

            public ISourceList<IPoeTradeViewModel> Items { get; }
        }

        private sealed class ItemListProxy : DisposableReactiveObject
        {
            public ItemListProxy(
                ReadOnlyObservableCollection<IPoeTradeViewModel> source, ISubject<Unit> updateRequestSubject)
            {
                Items = source
                    .ToObservableChangeSet()
                    .ToSourceList();
            }

            public ISourceList<IPoeTradeViewModel> Items { get; }
        }

        private sealed class TradeComparer : IComparer<IPoeTradeViewModel>
        {
            private readonly IComparer<IPoeTradeViewModel> comparer;

            public TradeComparer(params SortData[] descriptionData)
            {
                Guard.ArgumentNotNull(descriptionData, nameof(descriptionData));

                comparer = OrderedComparer
                    .For<IPoeTradeViewModel>()
                    .OrderBy(x => 0);

                foreach (var data in descriptionData.EmptyIfNull().Where(x => x?.Data.IsEmpty == false))
                {
                    comparer = data.Apply(comparer);
                }
            }

            public int Compare(IPoeTradeViewModel x, IPoeTradeViewModel y)
            {
                return comparer.Compare(x, y);
            }
        }

        private sealed class SortData
        {
            public SortDescriptionData Data { get; }

            private readonly Func<IPoeTradeViewModel, object> fieldExtractor;

            public SortData(SortDescriptionData data, Func<IPoeTradeViewModel, object> fieldExtractor)
            {
                this.Data = data;
                this.fieldExtractor = fieldExtractor;
            }

            public IComparer<IPoeTradeViewModel> Apply(IComparer<IPoeTradeViewModel> builder)
            {
                if (Data.Direction == ListSortDirection.Ascending)
                {
                    return builder.ThenBy(arg => fieldExtractor(arg));
                }

                return builder.ThenByDescending(arg => fieldExtractor(arg));
            }
        }
    }
}