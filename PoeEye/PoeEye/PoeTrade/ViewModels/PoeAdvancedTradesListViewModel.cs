﻿using System;
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
using Common.Logging;
using DynamicData;
using DynamicData.Binding;
using Guards;
using JetBrains.Annotations;
using PoeEye.Scaffolding;
using PoeShared;
using PoeShared.Common;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using PoeShared.UI.Models;
using ReactiveUI;
using Unity.Attributes;

namespace PoeEye.PoeTrade.ViewModels
{
    internal sealed class PoeAdvancedTradesListViewModel : DisposableReactiveObject, IPoeAdvancedTradesListViewModel
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PoeAdvancedTradesListViewModel));

        private static readonly Func<IPoeTradeViewModel, bool> AlwaysTruePredicate = model => true;
        private static readonly TimeSpan ResortRefilterThrottleTimeout = TimeSpan.FromMilliseconds(250);
        private static readonly TimeSpan DataChangesThrottleTimeout = TimeSpan.FromMilliseconds(1000);
        private readonly SerialDisposable activeFilterAnchor = new SerialDisposable();

        private readonly BehaviorSubject<Func<IPoeTradeViewModel, bool>> filterConditionSource = new BehaviorSubject<Func<IPoeTradeViewModel, bool>>(null);
        private readonly ISubject<Unit> resortRequest = new Subject<Unit>();

        private readonly ReadOnlyObservableCollection<IPoeTradeViewModel> itemsCollection;
        private readonly ReadOnlyObservableCollection<IPoeTradeViewModel> rawItemsCollection;

        private readonly SourceCache<SortData, SortDescriptionData> sortingCache = new SourceCache<SortData, SortDescriptionData>(x => x.Data);

        private readonly SourceList<SortData> sortingRules = new SourceList<SortData>();
        private readonly SourceList<ISourceList<IPoeTradeViewModel>> tradeLists = new SourceList<ISourceList<IPoeTradeViewModel>>();

        private long filterRequestsCount;
        private long sortRequestsCount;
        private bool scrollToTop;

        public PoeAdvancedTradesListViewModel(
            [NotNull] IPageParameterDataViewModel pageParameter,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));
            Guard.ArgumentNotNull(pageParameter, nameof(pageParameter));

            activeFilterAnchor.AddTo(Anchors);

            var comparerObservable = sortingRules
                                     .Connect()
                                     .ToUnit()
                                     .StartWith()
                                     .Select(x => new TradeComparer(sortingRules.Items.ToArray()))
                                     .ObserveOn(uiScheduler);

            PageParameter = pageParameter;
            pageParameter.WhenAnyValue(x => x.CurrentPage)
                         .ObserveOn(uiScheduler)
                         .Select(x => new { pageParameter.CurrentPage, pageParameter.TotalCount, pageParameter.PageCount, pageParameter.PageSize })
                         .Where(x => x.CurrentPage > 0 && x.TotalCount > 0 && x.PageCount > 0)
                         .Subscribe(x =>
                         {
                             Log.Debug($"Current page changed, list: {this}, current page: {x.CurrentPage} / {x.PageCount} (page size: {x.PageSize}, total count: {x.TotalCount})");
                             this.RaisePropertyChanged(nameof(ScrollToTop));
                         })
                         .AddTo(Anchors);

            var allItems = tradeLists
                .Or();

            allItems
                .ObserveOn(uiScheduler)
                .Bind(out rawItemsCollection)
                .Subscribe(_ => { }, Log.HandleUiException)
                .AddTo(Anchors);

            var pager = Observable.Merge(
                                      PageParameter.WhenAnyValue(x => x.CurrentPage).ToUnit(),
                                      PageParameter.WhenAnyValue(x => x.PageSize).ToUnit())
                                  .Select(x => new PageRequest(PageParameter.CurrentPage, PageParameter.PageSize))
                                  .Sample(ResortRefilterThrottleTimeout)
                                  .ObserveOn(uiScheduler);

            var filterSink = filterConditionSource.Select(x => x ?? AlwaysTruePredicate)
                                                  .Throttle(ResortRefilterThrottleTimeout)
                                                  .Do(_ => Interlocked.Increment(ref filterRequestsCount))
                                                  .ObserveOn(uiScheduler);
            var sortSink = resortRequest.Throttle(ResortRefilterThrottleTimeout)
                                        .Do(_ => Interlocked.Increment(ref sortRequestsCount))
                                        .ObserveOn(uiScheduler);
            rawItemsCollection
                .ToObservableChangeSet(x => x.Trade)
                .Filter(filterSink)
                .Sort(comparerObservable, sortSink)
                .Page(pager)
                .ObserveOn(uiScheduler)
                .Do(changes =>
                {
                    if (changes is IPagedChangeSet<IPoeTradeViewModel, IPoeItem> pageCacheChanges)
                    {
                        var response = new PageResponse(
                            pageCacheChanges.Response.PageSize,
                            pageCacheChanges.Response.TotalSize,
                            pageCacheChanges.Response.Page,
                            pageCacheChanges.Response.Pages);
                        PageParameter.Update(response);
                    }
                    else if (changes is IPageChangeSet<IPoeTradeViewModel> pageChanges)
                    {
                        //FIXME There is a bug in DynamicData - PageResponse for Lists is built incorrectly
                        var response = new PageResponse(
                            pageChanges.Response.PageSize,
                            pageChanges.Response.Page,
                            pageChanges.Response.TotalSize,
                            pageChanges.Response.Pages);
                        PageParameter.Update(response);
                    }
                })
                .Bind(out itemsCollection)
                .Subscribe(_ => { }, Log.HandleUiException)
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
                .Subscribe(() => filterConditionSource.OnNext(filterConditionSource.Value), Log.HandleUiException)
                .AddTo(Anchors);
        }

        public long FilterRequestsCount => filterRequestsCount;

        public long SortRequestsCount => sortRequestsCount;

        public bool ScrollToTop => PageParameter.CurrentPage > 0 && PageParameter.PageCount > 0 && PageParameter.TotalCount > 0;

        public void Add(ReadOnlyObservableCollection<IPoeTradeViewModel> itemList)
        {
            Guard.ArgumentNotNull(itemList, nameof(itemList));

            var proxy = new ItemListProxy(itemList).AddTo(Anchors);
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
            srcListChangeSet.Connect()
                            .OnItemAdded(item => tradeLists.Add(item))
                            .Subscribe(_ => { }, Log.HandleUiException)
                            .AddTo(Anchors);
            srcListChangeSet.Connect()
                            .OnItemRemoved(item => tradeLists.Remove(item))
                            .Subscribe(_ => { }, Log.HandleUiException)
                            .AddTo(Anchors);
        }

        public ReadOnlyObservableCollection<IPoeTradeViewModel> Items => itemsCollection;

        public ReadOnlyObservableCollection<IPoeTradeViewModel> RawItems => rawItemsCollection;
        
        public IPageParameterDataViewModel PageParameter { get; }

        public void ResetSorting()
        {
            if (Log.IsTraceEnabled)
            {
                Log.Trace($"Resetting sorting rules");
            }
            sortingRules.Clear();
        }

        public void Filter(IObservable<Predicate<IPoeTradeViewModel>> conditionSource)
        {
            conditionSource.Subscribe(condition => filterConditionSource.OnNext(new Func<IPoeTradeViewModel, bool>(condition)), Log.HandleUiException).AssignTo(activeFilterAnchor);
        }

        public void SortBy(string propertyName, ListSortDirection direction)
        {
            if (Log.IsTraceEnabled)
            {
                Log.Trace($"Sorting by {propertyName} {direction}");
            }

            ResetSorting();
            ThenSortBy(propertyName, direction);
        }

        public void ThenSortBy(string propertyName, ListSortDirection direction)
        {
            if (Log.IsTraceEnabled)
            {
                Log.Trace($"Then Sorting by {propertyName} {direction}");
            }

            var sort = new SortDescriptionData(propertyName, direction);
            var sortData = sortingCache.Lookup(sort);
            if (!sortData.HasValue)
            {
                throw new ApplicationException($"Could not find item with a key '{sort}', cache: ${sortingCache.Keys.DumpToTextRaw()}");
            }
            
            sortingRules.Add(sortData.Value);
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
                    .Select(x => x.ItemList.RawItems)
                    .Subscribe(
                        items =>
                        {
                            var proxy = new ItemListProxy(items).AssignTo(activeTradeListAnchors);
                            listOfItemLists.Clear();
                            listOfItemLists.Add(proxy.Items);
                        }, Log.HandleUiException)
                    .AddTo(Anchors);

                Items = listOfItemLists.Or().ToSourceList();

                Disposable.Create(() => Log.Trace($"[PoeAdvancedTradesListViewModel.TabProxy] Proxy for tab {tab} ({tab.TabName}) was disposed"))
                          .AddTo(Anchors);
            }

            public ISourceList<IPoeTradeViewModel> Items { get; }
        }

        private sealed class ItemListProxy : DisposableReactiveObject
        {
            public ItemListProxy(
                ReadOnlyObservableCollection<IPoeTradeViewModel> source)
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
            private readonly Func<IPoeTradeViewModel, object> fieldExtractor;

            public SortData(SortDescriptionData data, Func<IPoeTradeViewModel, object> fieldExtractor)
            {
                Data = data;
                this.fieldExtractor = fieldExtractor;
            }

            public SortDescriptionData Data { get; }

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