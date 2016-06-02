using System.Reactive.Disposables;
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
        private bool audioNotificationEnabled;
        private string tabName;

        private readonly SerialDisposable tradesListAnchors = new SerialDisposable();
        private IPoeTradesListViewModel tradesList;

        public MainWindowTabViewModel(
            [NotNull] IFactory<IPoeTradesListViewModel, IPoeApiWrapper> tradesListFactory,
            [NotNull] IAudioNotificationsManager audioNotificationsManager,
            [NotNull] IRecheckPeriodViewModel recheckPeriod,
            [NotNull] IPoeApiSelectorViewModel apiSelector,
            [NotNull] [Dependency(WellKnownWindows.Main)] IWindowTracker mainWindowTracker,
            [NotNull] IFactory<IPoeQueryViewModel, IPoeStaticData> queryFactory)
        {
            this.tradesListFactory = tradesListFactory;
            Guard.ArgumentNotNull(() => tradesListFactory);
            Guard.ArgumentNotNull(() => apiSelector);
            Guard.ArgumentNotNull(() => mainWindowTracker);
            Guard.ArgumentNotNull(() => audioNotificationsManager);
            Guard.ArgumentNotNull(() => queryFactory);

            tabHeader = $"Tab #{GlobalTabIdx++}";

            this.queryFactory = queryFactory;
            ApiSelector = apiSelector;
            apiSelector.AddTo(Anchors);

            tradesListAnchors.AddTo(Anchors);
            
            RecheckPeriod = recheckPeriod;

            searchCommand = ReactiveCommand.Create();
            searchCommand.Subscribe(SearchCommandExecute).AddTo(Anchors);

            markAllAsReadCommand = ReactiveCommand.Create();
            markAllAsReadCommand.Subscribe(MarkAllAsReadExecute);

            refreshCommand = ReactiveCommand.Create(this.WhenAnyValue(x => x.IsBusy).Select(x => !x));
            refreshCommand.Subscribe(RefreshCommandExecuted);

            apiSelector
                .WhenAnyValue(x => x.SelectedModule)
                .Subscribe(ReinitializeApi)
                .AddTo(Anchors);

            apiSelector
                .WhenAnyValue(x => x.SelectedModule)
                .Subscribe(() => this.RaisePropertyChanged(nameof(SelectedApi)))
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.NewItemsCount)
                .DistinctUntilChanged()
                .Where(x => x > 0)
                .Where(x => audioNotificationEnabled)
                .Where(x => !mainWindowTracker.IsActive)
                .Subscribe(x => audioNotificationsManager.PlayNotification(AudioNotificationType.NewItem), Log.HandleException)
                .AddTo(Anchors);

            Query.ObservableForProperty(x => x.PoeQueryBuilder)
                 .ToUnit()
                 .StartWith(Unit.Default)
                 .Subscribe(RebuildTabName)
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

        public IPoeTradesListViewModel TradesList {
            get { return tradesList; }
            private set { this.RaiseAndSetIfChanged(ref tradesList, value); }
        }

        public IRecheckPeriodViewModel RecheckPeriod { get; }

        public bool AudioNotificationEnabled
        {
            get { return audioNotificationEnabled; }
            set { this.RaiseAndSetIfChanged(ref audioNotificationEnabled, value); }
        }

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

            AudioNotificationEnabled = config.AudioNotificationEnabled;
        }

        public PoeEyeTabConfig Save()
        {
            return new PoeEyeTabConfig
            {
                RecheckTimeout = RecheckPeriod.Period,
                IsAutoRecheckEnabled = RecheckPeriod.IsAutoRecheckEnabled,
                QueryInfo = Query.PoeQueryBuilder(),
                AudioNotificationEnabled = AudioNotificationEnabled,
                SoldOrRemovedItems = TradesList.HistoricalTrades.ItemsViewModels.Select(x => x.Trade).ToArray(),
                ApiModuleName = ApiSelector.SelectedModule.Name,
            };
        }

        private void ReinitializeApi(IPoeApiWrapper api)
        {
            var anchors = new CompositeDisposable();
            tradesListAnchors.Disposable = anchors;

            if (api == null)
            {
                TradesList = null;
                Query = null;
            }
            else
            {
                TradesList = tradesListFactory.Create(api);
                tradesList.AddTo(anchors);

                Query = queryFactory.Create(api.StaticData);

                tradesList
                   .WhenAnyValue(x => x.IsBusy)
                   .Subscribe(() => this.RaisePropertyChanged(nameof(IsBusy)))
                   .AddTo(anchors);


                tradesList.Items.ItemChanged.ToUnit()
                          .Merge(tradesList.Items.Changed.ToUnit())
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
            RebuildTabName();
        }

        private void RebuildTabName()
        {
            Log.Instance.Debug($"[MainWindowTabViewModel.RebuildTabName] Rebuilding tab name, tabQueryMode: {Query}...");
            
            var queryDescription = Query.Description;
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
            RebuildTabName();
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