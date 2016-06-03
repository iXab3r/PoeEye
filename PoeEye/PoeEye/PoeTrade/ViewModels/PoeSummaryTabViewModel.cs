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

    internal sealed class PoeSummaryTabViewModel : DisposableReactiveObject
    {
        private readonly IDictionary<IMainWindowTabViewModel, TabInfo> collectionByTab = new Dictionary<IMainWindowTabViewModel, TabInfo>();

        private readonly ReactiveCommand<object> markAllAsReadCommand;

        private readonly ReactiveList<PoeFilteredTradeViewModel> tradesCollection = new ReactiveList<PoeFilteredTradeViewModel>
        {
            ChangeTrackingEnabled = true
        };

        private bool isGrouping;

        private bool showNewItems = true;

        private bool showRemovedItems;

        private SortDescriptionData activeSortDescriptionData;

        public PoeSummaryTabViewModel(
            [NotNull] IReactiveList<IMainWindowTabViewModel> tabsList)
        {
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

            source.LiveFilteringProperties.Add(null);
            source.LiveFilteringProperties.Add(string.Empty);

            TradesView = source.View;
            TradesView.Filter = TradeFilterPredicate;

            SortingOptions = new[]
            {
                new SortDescriptionData(nameof(PoeFilteredTradeViewModel.Timestamp), ListSortDirection.Descending),
                new SortDescriptionData(nameof(PoeFilteredTradeViewModel.Timestamp), ListSortDirection.Ascending),
                new SortDescriptionData(nameof(PoeFilteredTradeViewModel.PriceInChaosOrbs), ListSortDirection.Descending),
                new SortDescriptionData(nameof(PoeFilteredTradeViewModel.PriceInChaosOrbs), ListSortDirection.Ascending),
            };
            ActiveSortDescriptionData = SortingOptions.FirstOrDefault();

            this.WhenAnyValue(x => x.IsGrouping)
                .Subscribe(HandleGrouping)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.ActiveSortDescriptionData)
                .Subscribe(HandleSorting)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.ShowNewItems, x => x.ShowRemovedItems)
                .Subscribe(TradesView.Refresh)
                .AddTo(Anchors);

            tabsList.Changed
                    .Where(args => args != null)
                    .Subscribe(args => ProcessTabsCollectionChange(tabsList, args))
                    .AddTo(Anchors);
        }

        public ICollectionView TradesView { get; }

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

        public bool IsGrouping
        {
            get { return isGrouping; }
            set { this.RaiseAndSetIfChanged(ref isGrouping, value); }
        }

        public SortDescriptionData[] SortingOptions { get; }

        public SortDescriptionData ActiveSortDescriptionData
        {
            get { return activeSortDescriptionData; }
            set { this.RaiseAndSetIfChanged(ref activeSortDescriptionData, value); }
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

            tradeIsValid &= value.Owner.AudioNotificationSelector.SelectedValue != AudioNotificationType.Disabled;
            return tradeIsValid;
        }

        private void HandleSorting()
        {
            TradesView.SortDescriptions.Clear();

            if (activeSortDescriptionData != null)
            {
                TradesView.SortDescriptions.Add(activeSortDescriptionData.ToSortDescription());
            }

            TradesView.Refresh();
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
            var tabsToProcess = collectionByTab.Keys.Where(x => x.AudioNotificationSelector.SelectedValue != AudioNotificationType.Disabled).ToArray();
            tabsToProcess.ForEach(x => x.MarkAllAsReadCommand.Execute(null));
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
            var tabInfo = new TabInfo(tab);
            collectionByTab[tab] = tabInfo;

            tab
                .TradesList
                .Items
                .Changed
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
            ProcessResetTabTrades(tab, new IPoeTradeViewModel[0]);
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
                    ProcessResetTabTrades(tab, tab.TradesList.Items.ToArray());
                    break;
            }
        }

        private void ProcessAddTrade(IMainWindowTabViewModel tab, IPoeTradeViewModel[] tradesToAdd)
        {
            if (!tradesToAdd.Any())
            {
                return;
            }

            TabInfo tabInfo;
            if (!collectionByTab.TryGetValue(tab, out tabInfo))
            {
                return;
            }
            foreach (var poeTradeViewModel in tradesToAdd)
            {
                var view = tabInfo.AddTrade(poeTradeViewModel);
                tradesCollection.Add(view);
            }
        }

        private void ProcessRemoveTrade(IMainWindowTabViewModel tab, IPoeTradeViewModel[] tradesToRemove)
        {
            if (!tradesToRemove.Any())
            {
                return;
            }

            TabInfo tabInfo;
            if (!collectionByTab.TryGetValue(tab, out tabInfo))
            {
                return;
            }

            foreach (var poeTradeViewModel in tradesToRemove)
            {
                PoeFilteredTradeViewModel view;
                if (!tabInfo.TryRemoveTrade(poeTradeViewModel, out view))
                {
                    continue;
                }
                tradesCollection.Remove(view);
            }
        }

        private void ProcessResetTabTrades(IMainWindowTabViewModel tab, IPoeTradeViewModel[] actualTradesList)
        {
            TabInfo tabInfo;
            if (!collectionByTab.TryGetValue(tab, out tabInfo))
            {
                return;
            }

            var currentTradesList = tabInfo.Trades.ToArray();
            var tradesToAdd = actualTradesList.Except(currentTradesList).ToArray();
            var tradesToRemove = currentTradesList.Except(actualTradesList).ToArray();

            ProcessRemoveTrade(tab, tradesToRemove);
            ProcessAddTrade(tab, tradesToAdd);
        }

        private class TabInfo : IDisposable
        {
            public CompositeDisposable Anchors { get; } = new CompositeDisposable();

            public IEnumerable<IPoeTradeViewModel> Trades => viewByViewModelMap.Keys;

            private readonly IDictionary<IPoeTradeViewModel, PoeFilteredTradeViewModel> viewByViewModelMap = new Dictionary<IPoeTradeViewModel, PoeFilteredTradeViewModel>();

            private readonly IMainWindowTabViewModel tab;

            public TabInfo(IMainWindowTabViewModel tab)
            {
                this.tab = tab;
            }

            public PoeFilteredTradeViewModel AddTrade(IPoeTradeViewModel viewModel)
            {
                var result = viewByViewModelMap[viewModel] = new PoeFilteredTradeViewModel(tab, viewModel);
                Anchors.Add(result);
                return result;
            }

            public bool TryRemoveTrade(IPoeTradeViewModel viewModel, out PoeFilteredTradeViewModel view)
            {
                var result = viewByViewModelMap.TryGetValue(viewModel, out view);
                if (result)
                {
                   RemoveTrade(viewModel, view);
                }
                return result;
            }

            private void RemoveTrade(IPoeTradeViewModel viewModel, PoeFilteredTradeViewModel view)
            {
                viewByViewModelMap.Remove(viewModel);
                view.Dispose();
                Anchors.Remove(view);
            }

            public void Dispose()
            {
                Anchors.Dispose();
            }
        }
    }
}