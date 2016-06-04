﻿using System.Reactive.Disposables;
using PoeShared.PoeTrade.Query;
using PoeShared.Prism;

namespace PoeEye.PoeTrade.ViewModels
{
    using System;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Windows.Input;

    using Config;

    using Exceptionless;

    using Guards;

    using JetBrains.Annotations;

    using Microsoft.Practices.Unity;

    using Models;

    using PoeEye.Prism;

    using PoeShared;
    using PoeShared.PoeTrade;
    using PoeShared.Scaffolding;

    using ReactiveUI;

    internal sealed class MainWindowTabViewModel : DisposableReactiveObject, IMainWindowTabViewModel
    {
        private readonly IFactory<IPoeTradesListViewModel, IPoeApiWrapper> tradesListFactory;
        private readonly IFactory<IPoeQueryViewModel, IPoeStaticData> queryFactory;
        private static int GlobalTabIdx;


        private readonly ReactiveCommand<object> markAllAsReadCommand;
        private readonly ReactiveCommand<object> refreshCommand;
        private readonly ReactiveCommand<object> searchCommand;

        private readonly string tabHeader;

        private IPoeQueryViewModel query;
        private string tabName;

        private readonly SerialDisposable tradesListAnchors = new SerialDisposable();
        private IPoeTradesListViewModel tradesList;

        public MainWindowTabViewModel(
            [NotNull] IFactory<IPoeTradesListViewModel, IPoeApiWrapper> tradesListFactory,
            [NotNull] IAudioNotificationsManager audioNotificationsManager,
            [NotNull] IRecheckPeriodViewModel recheckPeriod,
            [NotNull] IPoeApiSelectorViewModel apiSelector,
            [NotNull] [Dependency(WellKnownWindows.Main)] IWindowTracker mainWindowTracker,
            [NotNull] IAudioNotificationSelectorViewModel audioNotificationSelector,
            [NotNull] IFactory<IPoeQueryViewModel, IPoeStaticData> queryFactory)
        {
            this.tradesListFactory = tradesListFactory;
            Guard.ArgumentNotNull(() => tradesListFactory);
            Guard.ArgumentNotNull(() => apiSelector);
            Guard.ArgumentNotNull(() => mainWindowTracker);
            Guard.ArgumentNotNull(() => audioNotificationsManager);
            Guard.ArgumentNotNull(() => audioNotificationSelector);
            Guard.ArgumentNotNull(() => queryFactory);

            tabHeader = $"Tab #{GlobalTabIdx++}";

            this.queryFactory = queryFactory;
            ApiSelector = apiSelector;
            apiSelector.AddTo(Anchors);

            tradesListAnchors.AddTo(Anchors);

            RecheckPeriod = recheckPeriod;

            AudioNotificationSelector = audioNotificationSelector;
            audioNotificationSelector.AddTo(Anchors);

            searchCommand = ReactiveCommand.Create();
            searchCommand.Subscribe(SearchCommandExecute).AddTo(Anchors);

            markAllAsReadCommand = ReactiveCommand.Create();
            markAllAsReadCommand.Subscribe(MarkAllAsReadExecute);

            refreshCommand = ReactiveCommand.Create(this.WhenAnyValue(x => x.IsBusy).Select(x => !x));
            refreshCommand.Subscribe(RefreshCommandExecuted);

            apiSelector
                .WhenAnyValue(x => x.SelectedModule)
                .Where(x => x != null)
                .Subscribe(ReinitializeApi)
                .AddTo(Anchors);

            apiSelector
                .WhenAnyValue(x => x.SelectedModule)
                .Subscribe(() => this.RaisePropertyChanged(nameof(SelectedApi)))
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.NewItemsCount)
                .DistinctUntilChanged()
                .Where(x => x > 0)
                .Where(x => audioNotificationSelector.SelectedValue != AudioNotificationType.Disabled)
                .Where(x => !mainWindowTracker.IsActive)
                .Subscribe(x => audioNotificationsManager.PlayNotification(AudioNotificationType.Whistle), Log.HandleException)
                .AddTo(Anchors);

            RecheckPeriod
                .WhenAny(x => x.Period, x => x.IsAutoRecheckEnabled, (x, y) => Unit.Default)
                .Where(x => TradesList != null)
                .Subscribe(x => TradesList.RecheckPeriod = RecheckPeriod.IsAutoRecheckEnabled ? RecheckPeriod.Period : TimeSpan.Zero)
                .AddTo(Anchors);
        }

        public IPoeApiSelectorViewModel ApiSelector { get; }

        public IPoeApiWrapper SelectedApi => ApiSelector.SelectedModule;

        public ICommand SearchCommand => searchCommand;

        public ICommand RefreshCommand => refreshCommand;

        public ICommand MarkAllAsReadCommand => markAllAsReadCommand;

        public bool IsBusy => TradesList.IsBusy;

        public string TabName
        {
            get { return tabName; }
            private set { this.RaiseAndSetIfChanged(ref tabName, value); }
        }

        public bool HasNewTrades => NewItemsCount > 0;

        public int NewItemsCount
        {
            get { return TradesList.Items.Count(x => x.TradeState == PoeTradeState.New); }
        }

        public int RemovedItemsCount
        {
            get { return TradesList.Items.Count(x => x.TradeState == PoeTradeState.Removed); }
        }

        public int NormalItemsCount
        {
            get { return TradesList.Items.Count(x => x.TradeState == PoeTradeState.Normal); }
        }

        public IPoeTradesListViewModel TradesList
        {
            get { return tradesList; }
            private set { this.RaiseAndSetIfChanged(ref tradesList, value); }
        }

        public IRecheckPeriodViewModel RecheckPeriod { get; }

        public IAudioNotificationSelectorViewModel AudioNotificationSelector { get; }

        public IPoeQueryViewModel Query
        {
            get { return query; }
            private set { this.RaiseAndSetIfChanged(ref query, value); }
        }

        public void Load(PoeEyeTabConfig config)
        {
            if (!string.IsNullOrWhiteSpace(config.ApiModuleName))
            {
                ApiSelector.SetByModuleName(config.ApiModuleName);
            }

            if (config.RecheckTimeout != default(TimeSpan))
            {
                RecheckPeriod.Period = config.RecheckTimeout;
                RecheckPeriod.IsAutoRecheckEnabled = config.IsAutoRecheckEnabled;
            }

            if (config.QueryInfo != null)
            {
                Query.SetQueryInfo(config.QueryInfo);
                Query.IsExpanded = true;
            }

            if (config.SoldOrRemovedItems != null)
            {
                TradesList.HistoricalTrades.Clear();
                TradesList.HistoricalTrades.AddItems(config.SoldOrRemovedItems);
            }

            AudioNotificationSelector.SelectedValue = config.NotificationType;
        }

        public PoeEyeTabConfig Save()
        {
            return new PoeEyeTabConfig
            {
                RecheckTimeout = RecheckPeriod.Period,
                IsAutoRecheckEnabled = RecheckPeriod.IsAutoRecheckEnabled,
                QueryInfo = Query.PoeQueryBuilder(),
                NotificationType = AudioNotificationSelector.SelectedValue,
                SoldOrRemovedItems = TradesList.HistoricalTrades.ItemsViewModels.Select(x => x.Trade).ToArray(),
                ApiModuleName = ApiSelector.SelectedModule.Name,
            };
        }

        private void ReinitializeApi(IPoeApiWrapper api)
        {
            Guard.ArgumentNotNull(() => api);
            var anchors = new CompositeDisposable();
            tradesListAnchors.Disposable = anchors;

            var existingQuery = query;
            var newQuery = queryFactory.Create(api.StaticData);
            if (existingQuery != null)
            {
                newQuery.SetQueryInfo(existingQuery);
            }
            Query = newQuery;

            TradesList?.Dispose();
            TradesList = tradesListFactory.Create(api);
            tradesList.AddTo(anchors);

            newQuery
                .ObservableForProperty(x => x.PoeQueryBuilder).ToUnit()
                .Merge(TradesList.WhenAnyValue(x => x.ActiveQuery).ToUnit())
                .Select(x => newQuery)
                .Subscribe(RebuildTabName)
                .AddTo(anchors);

            TradesList
               .WhenAnyValue(x => x.IsBusy).ToUnit()
               .StartWith(Unit.Default)
               .Subscribe(() => this.RaisePropertyChanged(nameof(IsBusy)))
               .AddTo(anchors);

            TradesList.Items.ItemChanged.ToUnit()
                      .Merge(tradesList.Items.Changed.ToUnit())
                      .StartWith(Unit.Default)
                      .Subscribe(
                          () =>
                          {
                              this.RaisePropertyChanged(nameof(NewItemsCount));
                              this.RaisePropertyChanged(nameof(RemovedItemsCount));
                              this.RaisePropertyChanged(nameof(NormalItemsCount));
                              this.RaisePropertyChanged(nameof(HasNewTrades));
                          })
                      .AddTo(anchors);
        }

        private void RebuildTabName(IPoeQueryViewModel queryToProcess)
        {
            Log.Instance.Debug($"[MainWindowTabViewModel.RebuildTabName] Rebuilding tab name, tabQueryMode: {queryToProcess}...");

            var queryDescription = queryToProcess.Description;
            TabName = string.IsNullOrWhiteSpace(queryDescription)
                ? tabHeader
                : $"{queryDescription}";
        }

        private void RefreshCommandExecuted(object arg)
        {
            TradesList.Refresh();
        }

        private void SearchCommandExecute(object arg)
        {
            var queryBuilder = arg as Func<IPoeQueryInfo>;
            if (queryBuilder == null)
            {
                return;
            }
            var query = queryBuilder();
            Log.Instance.Debug($"[MainWindowTabViewModel.SearchCommandExecute] Search command executed, running query\r\n{query.DumpToText()}");

            TradesList.Items.Clear();
            TradesList.ActiveQuery = query;
            Query.IsExpanded = false;

            if (TradesList.RecheckPeriod == TimeSpan.Zero)
            {
                Log.Instance.Debug($"[MainWindowTabViewModel.SearchCommandExecute] Auto-recheck is disabled, refreshing query manually...");
                TradesList.Refresh();
            }

            ExceptionlessClient.Default
                               .CreateFeatureUsage("TradeList")
                               .SetType("Search")
                               .SetMessage(Query.Description)
                               .SetProperty("Description", Query.Description)
                               .SetProperty("Query", query.DumpToText())
                               .Submit();
        }

        private void MarkAllAsReadExecute(object arg)
        {
            var tradesToAmend = TradesList.Items.Where(x => x.TradeState != PoeTradeState.Normal).ToArray();
            if (!tradesToAmend.Any())
            {
                return;
            }

            using (TradesList.Items.SuppressChangeNotifications())
            {
                foreach (var trade in tradesToAmend)
                {
                    trade.TradeState = PoeTradeState.Normal;
                }
            }
        }
    }
}