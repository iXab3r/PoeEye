namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using System.Reactive.Concurrency;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Windows.Data;
    using System.Windows.Input;

    using CsQuery.ExtensionMethods;

    using Guards;

    using JetBrains.Annotations;

    using Microsoft.Practices.Unity;

    using NuGet;

    using PoeShared.Scaffolding;

    using Prism;

    using ReactiveUI;

    internal sealed class PoeSummaryTabViewModel : DisposableReactiveObject
    {
        private readonly IDictionary<IMainWindowTabViewModel, TabInfo> collectionByTab = new Dictionary<IMainWindowTabViewModel, TabInfo>();

        private readonly ReactiveCommand<object> markAllAsReadCommand;

        private readonly ReactiveList<PoeFilteredTradeViewModel> tradesCollection = new ReactiveList<PoeFilteredTradeViewModel>
        {
            ChangeTrackingEnabled = true
        };

        private readonly IScheduler uiScheduler;

        private bool isGrouping = true;

        private bool showAllTabs;

        private bool showNewItems = true;

        private bool showRemovedItems;

        public PoeSummaryTabViewModel(
            [NotNull] IReactiveList<IMainWindowTabViewModel> tabsList,
            [NotNull] [Dependency(WellKnownSchedulers.Ui)] IScheduler uiScheduler)
        {
            this.uiScheduler = uiScheduler;
            Guard.ArgumentNotNull(() => tabsList);

            markAllAsReadCommand = ReactiveCommand.Create();
            markAllAsReadCommand.Subscribe(MarkAllAsReadExecuted).AddTo(Anchors);

            var source = new CollectionViewSource
            {
                IsLiveFilteringRequested = true,
                IsLiveSortingRequested = true,
                IsLiveGroupingRequested = true,
                Source = tradesCollection
            };

            source.SortDescriptions.Add(new SortDescription(nameof(PoeFilteredTradeViewModel.TradeState), ListSortDirection.Ascending));
            source.SortDescriptions.Add(new SortDescription(nameof(PoeFilteredTradeViewModel.PriceInChaosOrbs), ListSortDirection.Ascending));

            source.LiveFilteringProperties.Add(null);
            source.LiveFilteringProperties.Add(string.Empty);

            TradesView = source.View;
            TradesView.Filter = TradeFilterPredicate;

            this.WhenAnyValue(x => x.IsGrouping)
                .Subscribe(HandleGrouping)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.ShowAllTabs, x => x.ShowNewItems, x => x.ShowRemovedItems)
                .Subscribe(TradesView.Refresh)
                .AddTo(Anchors);

            tabsList.Changed
                    .Where(args => args != null)
                    .ObserveOn(uiScheduler)
                    .Subscribe(args => ProcessTabsCollectionChange(tabsList, args))
                    .AddTo(Anchors);
        }

        public ICollectionView TradesView { get; }

        public ICommand MarkAllAsReadCommand => markAllAsReadCommand;

        public bool ShowAllTabs
        {
            get { return showAllTabs; }
            set { this.RaiseAndSetIfChanged(ref showAllTabs, value); }
        }

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

        public bool IsGrouping
        {
            get { return isGrouping; }
            set { this.RaiseAndSetIfChanged(ref isGrouping, value); }
        }

        private bool TradeFilterPredicate(object value)
        {
            var trade = value as PoeFilteredTradeViewModel;
            return trade != null && TradeFilterPredicate(trade);
        }

        private bool TradeFilterPredicate(PoeFilteredTradeViewModel value)
        {
            var tradeIsValid = true;

            tradeIsValid &=
                (value.Trade.TradeState == PoeTradeState.Removed && showRemovedItems) ||
                (value.Trade.TradeState == PoeTradeState.New && showNewItems);

            tradeIsValid &= value.Owner.AudioNotificationEnabled || showAllTabs;
            return tradeIsValid;
        }

        private void HandleGrouping()
        {
            TradesView.GroupDescriptions.Clear();

            if (isGrouping)
            {
                var groupingByDescription = new PropertyGroupDescription(nameof(PoeFilteredTradeViewModel.Description));
                TradesView.GroupDescriptions.Add(groupingByDescription);
            }

            TradesView.Refresh();
        }

        private void MarkAllAsReadExecuted()
        {
            collectionByTab.Keys.ToArray().ForEach(x => x.MarkAllAsReadCommand.Execute(null));
        }

        private void ProcessTabsCollectionChange(IEnumerable<IMainWindowTabViewModel> tabs, NotifyCollectionChangedEventArgs args)
        {
            var newItems = args.NewItems?.Cast<IMainWindowTabViewModel>() ?? new IMainWindowTabViewModel[0];
            var oldItems = args.OldItems?.Cast<IMainWindowTabViewModel>() ?? new IMainWindowTabViewModel[0];
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    newItems.ForEach(ProcessAddTab);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    oldItems.ForEach(ProcessRemoveTab);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    oldItems.ForEach(ProcessRemoveTab);
                    newItems.ForEach(ProcessAddTab);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    collectionByTab.Keys.ToArray().ForEach(ProcessRemoveTab);
                    tabs.ForEach(ProcessAddTab);
                    break;
            }
        }

        private void ProcessAddTab(IMainWindowTabViewModel tab)
        {
            var tabInfo = new TabInfo();
            collectionByTab[tab] = tabInfo;

            tab
                .TradesList
                .Items
                .Changed
                .ObserveOn(uiScheduler)
                .Subscribe(args => ProcessCollectionChange(tab, args))
                .AddTo(tabInfo.Anchors);
        }

        private void ProcessRemoveTab(IMainWindowTabViewModel tab)
        {
            TabInfo tabInfo;
            if (!collectionByTab.TryGetValue(tab, out tabInfo))
            {
                return;
            }
            collectionByTab.Remove(tab);
            tabInfo.Dispose();
        }

        private void ProcessCollectionChange(IMainWindowTabViewModel tab, NotifyCollectionChangedEventArgs args)
        {
            var newItems = args.NewItems?.Cast<IPoeTradeViewModel>().ToArray() ?? new IPoeTradeViewModel[0];
            var oldItems = args.OldItems?.Cast<IPoeTradeViewModel>().ToArray() ?? new IPoeTradeViewModel[0];
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    ProcessAddTrade(tab, newItems);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    ProcessRemoveTrade(tab, oldItems);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    ProcessRemoveTrade(tab, oldItems);
                    ProcessAddTrade(tab, newItems);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    ProcessClearTabTrades(tab);
                    ProcessAddTrade(tab, tab.TradesList.Items.ToArray());
                    break;
            }
        }

        private void ProcessAddTrade(IMainWindowTabViewModel tab, IPoeTradeViewModel[] trades)
        {
            TabInfo tabInfo;
            if (!collectionByTab.TryGetValue(tab, out tabInfo))
            {
                return;
            }
            trades.ForEach(tabInfo.Trades.Add);
            trades.ForEach(x => tradesCollection.Add(new PoeFilteredTradeViewModel(tab, x)));
        }

        private void ProcessRemoveTrade(IMainWindowTabViewModel tab, IPoeTradeViewModel[] trades)
        {
            TabInfo tabInfo;
            if (!collectionByTab.TryGetValue(tab, out tabInfo))
            {
                return;
            }
            trades.ForEach(x => tabInfo.Trades.Remove(x));
            tradesCollection.RemoveAll(x => trades.Contains(x.Trade));
        }

        private void ProcessClearTabTrades(IMainWindowTabViewModel tab)
        {
            TabInfo tabInfo;
            if (!collectionByTab.TryGetValue(tab, out tabInfo))
            {
                return;
            }
            ProcessRemoveTrade(tab, tabInfo.Trades.ToArray());
        }

        private class TabInfo : IDisposable
        {
            public CompositeDisposable Anchors { get; } = new CompositeDisposable();

            public ICollection<IPoeTradeViewModel> Trades { get; } = new HashSet<IPoeTradeViewModel>();

            public void Dispose()
            {
                Anchors.Dispose();
            }
        }
    }
}