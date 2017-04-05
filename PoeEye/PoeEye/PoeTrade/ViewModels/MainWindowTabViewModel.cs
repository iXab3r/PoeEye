using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using DynamicData;
using DynamicData.Binding;
using Exceptionless;
using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeEye.Config;
using PoeEye.PoeTrade.Common;
using PoeEye.PoeTrade.Models;
using PoeShared;
using PoeShared.Audio;
using PoeShared.Common;
using PoeShared.PoeTrade;
using PoeShared.PoeTrade.Query;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.UI.ViewModels;
using ReactiveUI;
using ReactiveUI.Legacy;
using ReactiveCommand = ReactiveUI.ReactiveCommand;

namespace PoeEye.PoeTrade.ViewModels
{
    internal sealed class MainWindowTabViewModel : DisposableReactiveObject, IMainWindowTabViewModel
    {
        private static int GlobalTabIdx;

        private readonly ReactiveCommand<object> markAllAsReadCommand;
        private readonly IFactory<IPoeQueryViewModel, IPoeStaticData> queryFactory;
        private readonly ReactiveCommand<object> refreshCommand;
        private readonly ReactiveCommand<object> newSearchCommand;

        private readonly string tabHeader;

        private readonly SerialDisposable tradesListAnchors = new SerialDisposable();
        private readonly IFactory<IPoeTradesListViewModel, IPoeApiWrapper> tradesListFactory;

        private IPoeQueryViewModel query;
        private string tabName;
        private IPoeTradesListViewModel tradesList;

        public MainWindowTabViewModel(
            [NotNull] IFactory<IPoeTradesListViewModel, IPoeApiWrapper> tradesListFactory,
            [NotNull] IAudioNotificationsManager audioNotificationsManager,
            [NotNull] IRecheckPeriodViewModel recheckPeriod,
            [NotNull] IPoeApiSelectorViewModel apiSelector,
            [NotNull] [Dependency(WellKnownWindows.MainWindow)] IWindowTracker mainWindowTracker,
            [NotNull] IAudioNotificationSelectorViewModel audioNotificationSelector,
            [NotNull] IFactory<IPoeQueryViewModel, IPoeStaticData> queryFactory)
        {
            this.tradesListFactory = tradesListFactory;
            Guard.ArgumentNotNull(tradesListFactory, nameof(tradesListFactory));
            Guard.ArgumentNotNull(apiSelector, nameof(apiSelector));
            Guard.ArgumentNotNull(mainWindowTracker, nameof(mainWindowTracker));
            Guard.ArgumentNotNull(audioNotificationsManager, nameof(audioNotificationsManager));
            Guard.ArgumentNotNull(audioNotificationSelector, nameof(audioNotificationSelector));
            Guard.ArgumentNotNull(queryFactory, nameof(queryFactory));

            Id = tabHeader = $"Tab #{GlobalTabIdx++}";

            this.queryFactory = queryFactory;
            ApiSelector = apiSelector;
            apiSelector.AddTo(Anchors);

            tradesListAnchors.AddTo(Anchors);

            RecheckPeriod = recheckPeriod;

            AudioNotificationSelector = audioNotificationSelector;
            audioNotificationSelector.AddTo(Anchors);

            markAllAsReadCommand = ReactiveUI.Legacy.ReactiveCommand.Create();
            markAllAsReadCommand.Subscribe(MarkAllAsReadExecute);

            refreshCommand = ReactiveUI.Legacy.ReactiveCommand.Create(this.WhenAnyValue(x => x.IsBusy).Select(x => !x));
            refreshCommand.Subscribe(RefreshCommandExecuted);

            newSearchCommand = ReactiveUI.Legacy.ReactiveCommand.Create(this.WhenAnyValue(x => x.IsBusy).Select(x => !x));
            newSearchCommand.Subscribe(NewSearchCommandExecuted);

            apiSelector
                .WhenAnyValue(x => x.SelectedModule)
                .Where(x => x != null)
                .Subscribe(ReinitializeApi)
                .AddTo(Anchors);

            apiSelector
                .WhenAnyValue(x => x.SelectedModule)
                .Subscribe(() => this.RaisePropertyChanged(nameof(SelectedApi)))
                .AddTo(Anchors);

            this.BindPropertyTo(nameof(SelectedAudioNotificationType), audioNotificationSelector, x => x.SelectedValue)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.NewItemsCount)
                .DistinctUntilChanged()
                .Where(x => x > 0)
                .Where(x => audioNotificationSelector.SelectedValue != AudioNotificationType.Disabled)
                .Where(x => !mainWindowTracker.IsActive)
                .Subscribe(
                    x => audioNotificationsManager.PlayNotification(audioNotificationSelector.SelectedValue),
                    Log.HandleException)
                .AddTo(Anchors);
        }

        public IPoeApiSelectorViewModel ApiSelector { get; }

        public AudioNotificationType SelectedAudioNotificationType => AudioNotificationSelector.SelectedValue;

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

        public IPoeApiWrapper SelectedApi => ApiSelector.SelectedModule;

        public ICommand RefreshCommand => refreshCommand;

        public ICommand NewSearchCommand => newSearchCommand;

        public ICommand MarkAllAsReadCommand => markAllAsReadCommand;

        public bool IsBusy => TradesList.IsBusy;

        public string TabName
        {
            get { return tabName; }
            private set { this.RaiseAndSetIfChanged(ref tabName, value); }
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
            if (!string.IsNullOrWhiteSpace(config.ApiModuleId))
            {
                ApiSelector.SetByModuleId(config.ApiModuleId);
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

            AudioNotificationSelector.SelectedValue = config.NotificationType;
        }

        public PoeEyeTabConfig Save()
        {
            var query = Query;
            return new PoeEyeTabConfig
            {
                RecheckTimeout = RecheckPeriod.Period,
                IsAutoRecheckEnabled = RecheckPeriod.IsAutoRecheckEnabled,
                QueryInfo = query == null ? new PoeQueryInfo() : query.PoeQueryBuilder(),
                NotificationType = AudioNotificationSelector.SelectedValue,
                ApiModuleId = ApiSelector.SelectedModule.Id.ToString()
            };
        }

        private void ReinitializeApi(IPoeApiWrapper api)
        {
            Guard.ArgumentNotNull(api, nameof(api));
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
                .Merge(tradesList.WhenAnyValue(x => x.ActiveQuery).ToUnit())
                .Select(x => newQuery)
                .Subscribe(RebuildTabName)
                .AddTo(anchors);

            RecheckPeriod
                .WhenAny(x => x.Period, x => x.IsAutoRecheckEnabled, (x, y) => Unit.Default)
                .Subscribe(
                    x =>
                        tradesList.RecheckPeriod =
                            RecheckPeriod.IsAutoRecheckEnabled ? RecheckPeriod.Period : TimeSpan.Zero)
                .AddTo(anchors);

            tradesList
                .WhenAnyValue(x => x.IsBusy).ToUnit()
                .StartWith(Unit.Default)
                .Subscribe(() => this.RaisePropertyChanged(nameof(IsBusy)))
                .AddTo(anchors);

            Observable.Merge(
                    tradesList.Items.ToObservableChangeSet().ToUnit(),
                    tradesList.Items.ToObservableChangeSet().WhenPropertyChanged(x => x.TradeState).ToUnit()
                )
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
            Log.Instance.Debug(
                $"[MainWindowTabViewModel.RebuildTabName] Rebuilding tab name, tabQueryMode: {queryToProcess}...");

            var queryDescription = queryToProcess.Description;
            TabName = string.IsNullOrWhiteSpace(queryDescription)
                ? tabHeader
                : $"{queryDescription}";
        }

        private void NewSearchCommandExecuted(object arg)
        {
            TradesList.ActiveQuery = null;
            RefreshCommandExecuted(arg);
        }

        private void RefreshCommandExecuted(object arg)
        {
            var queryBuilder = arg as Func<IPoeQueryInfo>;
            if (TradesList.ActiveQuery == null && queryBuilder != null)
            {
                var query = queryBuilder();
                Log.Instance.Debug(
                    $"[MainWindowTabViewModel.RefreshCommandExecuted] Search command executed, running query\r\n{query.DumpToText()}");
                RunNewSearch(query);
            }
            else
            {
                Log.Instance.Debug(
                    $"[MainWindowTabViewModel.RefreshCommandExecuted] Refresh command executed, running query\r\n{query.DumpToText()}");
                TradesList.Refresh();
            }
        }

        private void RunNewSearch(IPoeQueryInfo query)
        {
            TradesList.Clear();
            TradesList.ActiveQuery = query;
            Query.IsExpanded = false;

            if (TradesList.RecheckPeriod == TimeSpan.Zero)
            {
                Log.Instance.Debug(
                    $"[MainWindowTabViewModel.SearchCommandExecute] Auto-recheck is disabled, refreshing query manually...");
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

            Log.Instance.Debug(
                $"[MainWindowTabViewModel.MarkAllAsReadExecute] Marking {tradesToAmend.Length} of {tradesList.Items.Count} item(s) as Read");

            foreach (var trade in tradesToAmend)
            {
                trade.TradeState = PoeTradeState.Normal;
            }
        }

        public string Id { get; } 
    }
}
