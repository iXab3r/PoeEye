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
using MaterialDesignThemes.Wpf;
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
using Prism.Commands;
using ReactiveUI;
using ReactiveUI.Legacy;

namespace PoeEye.PoeTrade.ViewModels
{
    internal sealed class MainWindowTabViewModel : DisposableReactiveObject, IMainWindowTabViewModel
    {
        private static int GlobalTabIdx;

        private readonly DelegateCommand markAllAsReadCommand;
        private readonly IFactory<IPoeQueryViewModel, IPoeStaticDataSource> queryFactory;
        private readonly DelegateCommand<object> refreshCommand;
        private readonly DelegateCommand<object> newSearchCommand;
        private readonly DelegateCommand<string> renameCommand;
        private readonly DelegateCommand resetCommand;

        private readonly string defaultTabName;

        private readonly SerialDisposable tradesListAnchors = new SerialDisposable();
        private readonly IFactory<IPoeTradesListViewModel, IPoeApiWrapper> tradesListFactory;
        private readonly Fallback<string> tabName = new Fallback<string>();

        private IPoeQueryViewModel query;
        private IPoeTradesListViewModel tradesList;
        private bool isFlipped;

        public MainWindowTabViewModel(
            [NotNull] IFactory<IPoeTradesListViewModel, IPoeApiWrapper> tradesListFactory,
            [NotNull] IAudioNotificationsManager audioNotificationsManager,
            [NotNull] IRecheckPeriodViewModel recheckPeriod,
            [NotNull] IPoeApiSelectorViewModel apiSelector,
            [NotNull] [Dependency(WellKnownWindows.MainWindow)] IWindowTracker mainWindowTracker,
            [NotNull] IAudioNotificationSelectorViewModel audioNotificationSelector,
            [NotNull] IFactory<IPoeQueryViewModel, IPoeStaticDataSource> queryFactory)
        {
            Guard.ArgumentNotNull(tradesListFactory, nameof(tradesListFactory));
            Guard.ArgumentNotNull(apiSelector, nameof(apiSelector));
            Guard.ArgumentNotNull(mainWindowTracker, nameof(mainWindowTracker));
            Guard.ArgumentNotNull(audioNotificationsManager, nameof(audioNotificationsManager));
            Guard.ArgumentNotNull(audioNotificationSelector, nameof(audioNotificationSelector));
            Guard.ArgumentNotNull(queryFactory, nameof(queryFactory));

            Id = defaultTabName = $"Tab #{GlobalTabIdx++}";
            tabName.SetDefaultValue(defaultTabName);

            this.tradesListFactory = tradesListFactory;
            this.BindPropertyTo(x => x.TabName, tabName, x => x.Value).AddTo(Anchors);
            this.BindPropertyTo(x => x.DefaultTabName, tabName, x => x.DefaultValue).AddTo(Anchors);

            this.queryFactory = queryFactory;
            ApiSelector = apiSelector;
            apiSelector.AddTo(Anchors);

            tradesListAnchors.AddTo(Anchors);

            RecheckPeriod = recheckPeriod;

            AudioNotificationSelector = audioNotificationSelector;
            audioNotificationSelector.AddTo(Anchors);

            renameCommand = new DelegateCommand<string>(RenameCommandExecuted);
            resetCommand = new DelegateCommand(ResetCommandExecuted, ResetCommandCanExecute);
            this.WhenAnyValue(x => x.SelectedApi).Subscribe(() => resetCommand.RaiseCanExecuteChanged()).AddTo(Anchors);

            markAllAsReadCommand = new DelegateCommand(MarkAllAsReadExecute);

            refreshCommand = new DelegateCommand<object>(RefreshCommandExecuted, RefreshCommandCanExecute);
            newSearchCommand = new DelegateCommand<object>(NewSearchCommandExecuted, NewSearchCommandCanExecute);
            this.WhenAnyValue(x => x.IsBusy, x => x.SelectedApi)
                .Subscribe(
                    () =>
                    {
                        refreshCommand.RaiseCanExecuteChanged();
                        newSearchCommand.RaiseCanExecuteChanged();
                    })
                .AddTo(Anchors);
            
            this
                .WhenAnyValue(x => x.SelectedApi)
                .Where(x => x != null)
                .Subscribe(ReinitializeApi)
                .AddTo(Anchors);

            this.BindPropertyTo(x => x.SelectedApi, apiSelector, x => x.SelectedModule).AddTo(Anchors);
            this.BindPropertyTo(x => x.SelectedAudioNotificationType, audioNotificationSelector, x => x.SelectedValue).AddTo(Anchors);
            
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
        private bool NewSearchCommandCanExecute(object arg)
        {
            return (SelectedApi?.IsAvailable ?? false);
        }

        private bool RefreshCommandCanExecute(object arg)
        {
            return !IsBusy && NewSearchCommandCanExecute(arg);
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

        public ICommand RenameCommand => renameCommand;
        
        public ICommand ResetCommand => resetCommand;

        public bool IsBusy => TradesList?.IsBusy ?? false;

        public string TabName => tabName.Value;

        public string DefaultTabName => tabName.DefaultValue;

        public IPoeTradesListViewModel TradesList
        {
            get { return tradesList; }
            private set { this.RaiseAndSetIfChanged(ref tradesList, value); }
        }

        public IRecheckPeriodViewModel RecheckPeriod { get; }

        public IAudioNotificationSelectorViewModel AudioNotificationSelector { get; }

        public bool IsFlipped
        {
            get { return isFlipped; }
            set { this.RaiseAndSetIfChanged(ref isFlipped, value); }
        }

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
            RenameTabTo(config.CustomTabName);
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
                ApiModuleId = ApiSelector.SelectedModule.Id.ToString(),
                CustomTabName = tabName.HasValue ? tabName.Value : null
            };
        }
        
        private void ResetCommandExecuted()
        {
            Log.Instance.Debug($"Resetting query parameters of tab {tabName.Value}");
            Query = queryFactory.Create(SelectedApi);
            ReinitializeApi(SelectedApi);
        }
        
        private bool ResetCommandCanExecute()
        {
            return SelectedApi != null;
        }

        private void RenameCommandExecuted(string value)
        {
            if (IsFlipped)
            {
                if (value == null)
                {
                    // Cancel
                } else if (string.IsNullOrWhiteSpace(value))
                {
                    RenameTabTo(default(string));
                }
                else
                {
                    RenameTabTo(value);
                }
            }
            
            IsFlipped = !IsFlipped;
        }

        private void RenameTabTo(string newTabNameOrDefault)
        {
            if (newTabNameOrDefault == tabName.Value)
            {
                return;
            }
            var previousValue = tabName.Value;
            tabName.SetValue(newTabNameOrDefault);
            Log.Instance.Debug($"Changed name of tab {defaultTabName}, {previousValue} => {tabName.Value}");
        }

        private void ReinitializeApi(IPoeApiWrapper api)
        {
            Guard.ArgumentNotNull(api, nameof(api));
            var anchors = new CompositeDisposable();
            tradesListAnchors.Disposable = anchors;
            TradesList?.Dispose();
            
            var existingQuery = Query;
            var newQuery = queryFactory.Create(api);
            if (existingQuery != null)
            {
                newQuery.SetQueryInfo(existingQuery);
            }
            Query = newQuery;

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

            var defaultTabName = string.IsNullOrWhiteSpace(queryDescription)
                ? this.defaultTabName
                : $"{queryDescription}";

            tabName.SetDefaultValue(defaultTabName);
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

        private void ResetTradesList()
        {
            TradesList.Clear();
            TradesList.ActiveQuery = null;
        }

        private void RunNewSearch(IPoeQueryInfo query)
        {
            ResetTradesList();
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

        private void MarkAllAsReadExecute()
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
