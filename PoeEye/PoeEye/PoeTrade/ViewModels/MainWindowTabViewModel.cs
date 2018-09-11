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
using PoeEye.Config;
using PoeShared;
using PoeShared.Audio;
using PoeShared.Common;
using PoeShared.PoeTrade;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.UI.ViewModels;
using Prism.Commands;
using ReactiveUI;
using Unity.Attributes;

namespace PoeEye.PoeTrade.ViewModels
{
    internal sealed class MainWindowTabViewModel : DisposableReactiveObject, IMainWindowTabViewModel
    {
        private static int GlobalTabIdx;

        private readonly string defaultTabName;

        private readonly DelegateCommand markAllAsReadCommand;
        private readonly DelegateCommand<object> newSearchCommand;
        private readonly DelegateCommand<object> refreshCommand;
        private readonly DelegateCommand<string> renameCommand;
        private readonly DelegateCommand resetCommand;
        private readonly Fallback<string> tabName = new Fallback<string>();

        private readonly SerialDisposable tradesListAnchors = new SerialDisposable();
        private readonly IFactory<IPoeTradesListViewModel, IPoeApiWrapper> tradesListFactory;
        private bool isFlipped;

        private IPoeTradesListViewModel tradesList;

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

            ApiSelector = apiSelector;
            apiSelector.AddTo(Anchors);

            tradesListAnchors.AddTo(Anchors);

            RecheckPeriod = recheckPeriod;

            AudioNotificationSelector = audioNotificationSelector;
            audioNotificationSelector.AddTo(Anchors);

            Query = queryFactory.Create(ApiSelector);

            renameCommand = new DelegateCommand<string>(RenameCommandExecuted);
            resetCommand = new DelegateCommand(ResetCommandExecuted, ResetCommandCanExecute);
            this.WhenAnyValue(x => x.SelectedApi).Subscribe(() => resetCommand.RaiseCanExecuteChanged()).AddTo(Anchors);

            markAllAsReadCommand = new DelegateCommand(MarkAllAsReadExecute);

            refreshCommand = new DelegateCommand<object>(RefreshCommandExecuted, RefreshCommandCanExecute);
            newSearchCommand = new DelegateCommand<object>(NewSearchCommandExecuted, NewSearchCommandCanExecute);

            Observable.Merge(
                          this.WhenAnyValue(x => x.IsBusy, x => x.SelectedApi).ToUnit(),
                          this.WhenAnyValue(x => x.SelectedApi).Select(x => x == null ? Observable.Never<bool>() : x.WhenAnyValue(y => y.IsAvailable)).Switch()
                              .ToUnit())
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
                .WithPrevious((prev, curr) => new {prev, curr})
                .Where(x => x.curr > x.prev)
                .Where(x => audioNotificationSelector.SelectedValue != AudioNotificationType.Disabled)
                .Where(x => !mainWindowTracker.IsActive)
                .Subscribe(
                    x => audioNotificationsManager.PlayNotification(audioNotificationSelector.SelectedValue),
                    Log.HandleException)
                .AddTo(Anchors);
        }

        public IPoeApiSelectorViewModel ApiSelector { get; }

        public bool HasNewTrades => NewItemsCount > 0;

        public int NewItemsCount
        {
            get { return TradesList?.Items.Count(x => x.TradeState == PoeTradeState.New) ?? 0; }
        }

        public int RemovedItemsCount
        {
            get { return TradesList?.Items.Count(x => x.TradeState == PoeTradeState.Removed) ?? 0; }
        }

        public int NormalItemsCount
        {
            get { return TradesList?.Items.Count(x => x.TradeState == PoeTradeState.Normal) ?? 0; }
        }

        public ICommand NewSearchCommand => newSearchCommand;

        public ICommand ResetCommand => resetCommand;

        public string DefaultTabName => tabName.DefaultValue;

        public IAudioNotificationSelectorViewModel AudioNotificationSelector { get; }

        public AudioNotificationType SelectedAudioNotificationType => AudioNotificationSelector.SelectedValue;

        public string Id { get; }

        public IPoeApiWrapper SelectedApi => ApiSelector.SelectedModule;

        public ICommand RefreshCommand => refreshCommand;

        public ICommand MarkAllAsReadCommand => markAllAsReadCommand;

        public ICommand RenameCommand => renameCommand;

        public bool IsBusy => TradesList?.IsBusy ?? false;

        public string TabName => tabName.Value;

        public IPoeTradesListViewModel TradesList
        {
            get => tradesList;
            private set => this.RaiseAndSetIfChanged(ref tradesList, value);
        }

        public IRecheckPeriodViewModel RecheckPeriod { get; }

        public bool IsFlipped
        {
            get => isFlipped;
            set => this.RaiseAndSetIfChanged(ref isFlipped, value);
        }

        public IPoeQueryViewModel Query { get; }

        public void Load(PoeEyeTabConfig config)
        {
            var defaultModule = ApiSelector.ModulesList.FirstOrDefault();
            if (ApiSelector.SetByModuleId(config.ApiModuleId) == null && defaultModule != null)
            {
                Log.Instance.Warn($"Failed to find module {config.ApiModuleId}, available modules: {ApiSelector.ModulesList.DumpToTextRaw()}");
                ApiSelector.SetByModuleId(defaultModule.Id.ToString());
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
            return new PoeEyeTabConfig
            {
                RecheckTimeout = RecheckPeriod.Period,
                IsAutoRecheckEnabled = RecheckPeriod.IsAutoRecheckEnabled,
                QueryInfo = Query.PoeQueryBuilder(),
                NotificationType = AudioNotificationSelector.SelectedValue,
                ApiModuleId = ApiSelector.SelectedModule?.Id.ToString(),
                CustomTabName = tabName.HasValue ? tabName.Value : null,
            };
        }

        private bool NewSearchCommandCanExecute(object arg)
        {
            return SelectedApi?.IsAvailable ?? false;
        }

        private bool RefreshCommandCanExecute(object arg)
        {
            return !IsBusy && NewSearchCommandCanExecute(arg);
        }

        private void ResetCommandExecuted()
        {
            Log.Instance.Debug($"Resetting query parameters of tab {tabName.Value}");
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
                }
                else if (string.IsNullOrWhiteSpace(value))
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

            TradesList = tradesListFactory.Create(api);
            tradesList.AddTo(anchors);

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
            
            Query
                .ObservableForProperty(x => x.PoeQueryBuilder).ToUnit()
                .Merge(tradesList.WhenAnyValue(x => x.ActiveQuery).ToUnit())
                .Select(x => Query)
                .Subscribe(RebuildTabName)
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
            Log.Instance.Debug($"[MainWindowTabViewModel.RebuildTabName] Rebuilding tab name, tabQueryMode: {queryToProcess}...");

            var queryDescription = queryToProcess.Description;

            tabName.SetDefaultValue(string.IsNullOrWhiteSpace(queryDescription)
                                        ? defaultTabName
                                        : $"{queryDescription}");
        }

        private void NewSearchCommandExecuted(object arg)
        {
            TradesList.ActiveQuery = null;
            RefreshCommandExecuted(arg);
        }

        private void RefreshCommandExecuted(object arg)
        {
            if (TradesList.ActiveQuery == null && arg is Func<IPoeQueryInfo> queryBuilder)
            {
                var newQuery = queryBuilder();
                Log.Instance.Debug(
                    $"[MainWindowTabViewModel.RefreshCommandExecuted] Search command executed, running query\r\n{newQuery.DumpToText()}");
                RunNewSearch(newQuery);
            }
            else
            {
                Log.Instance.Debug(
                    $"[MainWindowTabViewModel.RefreshCommandExecuted] Refresh command executed, running query\r\n{Query.DumpToText()}");
                TradesList.Refresh();
            }
        }

        private void ResetTradesList()
        {
            TradesList.Clear();
            TradesList.ActiveQuery = null;
        }

        private void RunNewSearch(IPoeQueryInfo newQuery)
        {
            ResetTradesList();
            TradesList.ActiveQuery = newQuery;
            Query.IsExpanded = false;

            if (TradesList.RecheckPeriod == TimeSpan.Zero)
            {
                Log.Instance.Debug("[MainWindowTabViewModel.SearchCommandExecute] Auto-recheck is disabled, refreshing query manually...");
                TradesList.Refresh();
            }

            ExceptionlessClient.Default
                               .CreateFeatureUsage("TradeList")
                               .SetType("Search")
                               .SetMessage(Query.Description)
                               .SetProperty("Description", Query.Description)
                               .SetProperty("Query", newQuery.DumpToText())
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

        public override string ToString()
        {
            return $"{Id} TabName: {TabName}";
        }
    }
}