using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using DynamicData;
using DynamicData.Alias;
using DynamicData.Binding;
using DynamicData.Controllers;
using Microsoft.Practices.Unity;
using PoeShared;
using PoeShared.Audio;
using PoeShared.Prism;
using ReactiveUI.Legacy;

namespace PoeEye.PoeTrade.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Windows.Data;
    using System.Windows.Input;

    using Common;

    using CsQuery.ExtensionMethods;

    using Guards;

    using JetBrains.Annotations;

    using NuGet;

    using PoeShared.Scaffolding;

    using ReactiveUI;

    internal sealed class PoeSummaryTabViewModel : DisposableReactiveObject, IPoeSummaryTabViewModel
    {
        private readonly ReactiveCommand markAllAsReadCommand;

        private readonly ReadOnlyObservableCollection<PoeFilteredTradeViewModel> tradesCollection;

        private bool showNewItems = true;

        private bool showRemovedItems;

        private SortDescriptionData activeSortDescriptionData;

        private readonly ISubject<Unit> rebuildFilterRequest = new Subject<Unit>();
        private readonly ISubject<Unit> sortRequest = new Subject<Unit>();

        public PoeSummaryTabViewModel(
            [NotNull] ReadOnlyObservableCollection<IMainWindowTabViewModel> tabsList,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(() => tabsList);

            markAllAsReadCommand = ReactiveCommand.Create(MarkAllAsReadExecuted);

            this.WhenAnyValue(x => x.ActiveSortDescriptionData)
                .ToUnit()
                .Subscribe(sortRequest)
                .AddTo(Anchors);

            Observable.Merge(
                    this.WhenAnyValue(x => x.ShowNewItems).ToUnit(),
                    this.WhenAnyValue(x => x.ShowRemovedItems).ToUnit()
                )
                .Subscribe(rebuildFilterRequest)
                .AddTo(Anchors);

            var srcListChangeSet =
                tabsList
                    .ToObservableChangeSet()
                    .Transform(tab => new TabProxy(tab, rebuildFilterRequest))
                    .DisposeMany()
                    .Transform(x => x.Trades)
                    .DisposeMany();

            var models = new SourceList<ISourceList<PoeFilteredTradeViewModel>>(srcListChangeSet);
                models
                    .Or()
                    .Filter(rebuildFilterRequest.StartWith().Select(_ => new Func<PoeFilteredTradeViewModel, bool>(TradeFilterPredicate)))
                    .Sort(sortRequest.StartWith().Select(x => new TradeComparer(activeSortDescriptionData)))
                    .ObserveOn(uiScheduler)
                    .Bind(out tradesCollection)
                    .Subscribe()
                    .AddTo(Anchors);

            SortingOptions = new[]
            {
                new SortDescriptionData(nameof(PoeFilteredTradeViewModel.Timestamp), ListSortDirection.Descending),
                new SortDescriptionData(nameof(PoeFilteredTradeViewModel.Timestamp), ListSortDirection.Ascending),
                new SortDescriptionData(nameof(PoeFilteredTradeViewModel.PriceInChaosOrbs), ListSortDirection.Descending),
                new SortDescriptionData(nameof(PoeFilteredTradeViewModel.PriceInChaosOrbs), ListSortDirection.Ascending),
            };
            ActiveSortDescriptionData = SortingOptions.FirstOrDefault();
        }

        public IEnumerable<PoeFilteredTradeViewModel> TradesView => tradesCollection;

        public ICommand MarkAllAsReadCommand => markAllAsReadCommand;

        public bool ShowNewItems
        {
            get { return showNewItems; }
            set { this.RaiseAndSetIfChanged(ref showNewItems, value); }
        }

        public bool ShowRemovedItems
        {
            get { return showRemovedItems; }
            set { this.RaiseAndSetIfChanged(ref showRemovedItems, value); }
        }

        public SortDescriptionData[] SortingOptions { get; }

        public SortDescriptionData ActiveSortDescriptionData
        {
            get { return activeSortDescriptionData; }
            set { this.RaiseAndSetIfChanged(ref activeSortDescriptionData, value); }
        }

        private bool TradeFilterPredicate(PoeFilteredTradeViewModel value)
        {
            var tradeIsValid = true;

            tradeIsValid &=
                (value.Trade.TradeState == PoeTradeState.Removed && showRemovedItems) ||
                (value.Trade.TradeState == PoeTradeState.New && showNewItems);

            tradeIsValid &= value.Owner.SelectedAudioNotificationType != AudioNotificationType.Disabled;
            return tradeIsValid;
        }

        private void MarkAllAsReadExecuted()
        {
            var tabsToProcess = tradesCollection
                .Where(x => x.Owner.SelectedAudioNotificationType != AudioNotificationType.Disabled)
                .ToArray();

            Log.Instance.Debug($"[PoeSummaryTabViewModel.MarkAllAsReadExecuted] Sending command to {tabsToProcess.Length} tab(s)");
            tabsToProcess.ForEach(x => x.Owner.MarkAllAsReadCommand.Execute(null));
        }

        private sealed class TabProxy : DisposableReactiveObject
        {
            public TabProxy(
                IMainWindowTabViewModel tab, ISubject<Unit> filterRequestSubject)
            {
                Guard.ArgumentNotNull(() => tab);
                Guard.ArgumentNotNull(() => filterRequestSubject);

                var listOfTradesList = new SourceList<ISourceList<PoeFilteredTradeViewModel>>();

                tab.WhenAnyValue(x => x.SelectedAudioNotificationType)
                   .ToUnit()
                   .Subscribe(filterRequestSubject)
                   .AddTo(Anchors);

                var activeTradeListAnchors = new SerialDisposable();
                tab
                    .WhenAnyValue(x => x.TradesList)
                    .Select(x => x.Items)
                    .Subscribe(
                        tradesList =>
                        {
                            var tradesListAnchors = new CompositeDisposable().AssignTo(activeTradeListAnchors);

                            tradesList
                                .ToObservableChangeSet()
                                .WhenPropertyChanged(x => x.TradeState)
                                .ToUnit()
                                .Subscribe(filterRequestSubject)
                                .AddTo(tradesListAnchors);

                            var tradesSet = tradesList
                                .ToObservableChangeSet()
                                .Transform(x => new PoeFilteredTradeViewModel(tab, x))
                                .DisposeMany()
                                .ToSourceList();

                            listOfTradesList.Clear();
                            listOfTradesList.Add(tradesSet);
                        })
                    .AddTo(Anchors);

                Trades = listOfTradesList.Or().ToSourceList();

                Disposable.Create(() => Log.Instance.Trace($"[PoeSummaryTabViewModel.TabProxy] Proxy for tab {tab} ({tab.TabName}) was disposed")).AddTo(Anchors);
            }

            public ISourceList<PoeFilteredTradeViewModel> Trades { get; }
        }

        private sealed class TradeComparer : IComparer<PoeFilteredTradeViewModel>
        {
            private readonly IComparer<PoeFilteredTradeViewModel> comparer;

            public TradeComparer(params SortDescriptionData[] descriptionData)
            {
                Guard.ArgumentNotNull(() => descriptionData);

                comparer = OrderedComparer
                    .For<PoeFilteredTradeViewModel>()
                    .OrderBy(x => string.Empty);

                foreach (var data in descriptionData.EmptyIfNull().Where(x => x != null))
                {
                    switch (data.PropertyName)
                    {
                        case nameof(PoeFilteredTradeViewModel.Timestamp):
                            comparer = OrderByField(comparer, data.Direction, x => x.Timestamp);
                            break;
                        case nameof(PoeFilteredTradeViewModel.PriceInChaosOrbs):
                            comparer = OrderByField(comparer, data.Direction, x => x.PriceInChaosOrbs);
                            break;
                    }
                }
            }

            private IComparer<T> OrderByField<T, TValue>(IComparer<T> builder, ListSortDirection direction, Func<T, TValue> selector)
            {
                if (direction == ListSortDirection.Ascending)
                {
                    return builder.ThenBy(selector);
                }
                return builder.ThenByDescending(selector);
            }

            public int Compare(PoeFilteredTradeViewModel x, PoeFilteredTradeViewModel y)
            {
                return comparer.Compare(x, y);
            }
        }
    }
}