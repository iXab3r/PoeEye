namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Windows.Input;

    using Guards;

    using JetBrains.Annotations;

    using Microsoft.Practices.Unity;

    using Models;

    using PoeShared;
    using PoeShared.PoeTrade;
    using PoeShared.Utilities;

    using Prism;

    using ReactiveUI;

    internal sealed class MainWindowTabViewModel : DisposableReactiveObject, IMainWindowTabViewModel
    {
        private static int GlobalTabIdx;

        private readonly ReactiveCommand<object> markAllAsRead;
        private readonly ReactiveCommand<object> refreshCommand;
        private readonly ReactiveCommand<object> searchCommand;

        private readonly string tabHeader;

        private bool audioNotificationEnabled;
        private string tabName;

        public MainWindowTabViewModel(
            [NotNull] PoeTradesListViewModel tradesList,
            [NotNull] IAudioNotificationsManager audioNotificationsManager,
            [NotNull] IRecheckPeriodViewModel recheckPeriod,
            [NotNull] [Dependency(WellKnownWindows.Main)] IWindowTracker mainWindowTracker,
            [NotNull] PoeQueryViewModel query)
        {
            Guard.ArgumentNotNull(() => tradesList);
            Guard.ArgumentNotNull(() => mainWindowTracker);
            Guard.ArgumentNotNull(() => audioNotificationsManager);
            Guard.ArgumentNotNull(() => query);

            tabHeader = $"Tab #{GlobalTabIdx++}";

            TradesList = tradesList;
            tradesList.AddTo(Anchors);

            RecheckPeriod = recheckPeriod;

            searchCommand = ReactiveCommand.Create();
            searchCommand.Subscribe(SearchCommandExecute);

            markAllAsRead = ReactiveCommand.Create();
            markAllAsRead.Subscribe(MarkAllAsReadExecute);

            refreshCommand = ReactiveCommand.Create(TradesList.WhenAnyValue(x => x.IsBusy).Select(x => !x));
            refreshCommand.Subscribe(RefreshCommandExecuted);

            tradesList
                .WhenAnyValue(x => x.IsBusy)
                .Subscribe(() => this.RaisePropertyChanged(nameof(IsBusy)))
                .AddTo(Anchors);

            Query = query;

            TradesList.TradesList.ItemChanged.ToUnit()
                      .Merge(TradesList.TradesList.Changed.ToUnit())
                      .Subscribe(
                          () =>
                          {
                              this.RaisePropertyChanged(nameof(NewItemsCount));
                              this.RaisePropertyChanged(nameof(RemovedItemsCount));
                              this.RaisePropertyChanged(nameof(NormalItemsCount));
                              this.RaisePropertyChanged(nameof(HasNewTrades));
                          })
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
                .WhenAny(x => x.RecheckValue, x => x.IsAutoRecheckEnabled, (x, y) => Unit.Default)
                .Subscribe(x => TradesList.RecheckPeriod = RecheckPeriod.IsAutoRecheckEnabled ? RecheckPeriod.RecheckValue : TimeSpan.Zero)
                .AddTo(Anchors);
        }

        public ICommand SearchCommand => searchCommand;

        public ICommand RefreshCommand => refreshCommand;

        public ICommand MarkAllAsRead => markAllAsRead;

        public bool IsBusy => TradesList.IsBusy;

        public string TabName
        {
            get { return tabName; }
            private set { this.RaiseAndSetIfChanged(ref tabName, value); }
        }

        public bool HasNewTrades => NewItemsCount > 0;

        public int NewItemsCount
        {
            get { return TradesList.TradesList.Count(x => x.TradeState == PoeTradeState.New); }
        }

        public int RemovedItemsCount
        {
            get { return TradesList.TradesList.Count(x => x.TradeState == PoeTradeState.Removed); }
        }

        public int NormalItemsCount
        {
            get { return TradesList.TradesList.Count(x => x.TradeState == PoeTradeState.Normal); }
        }

        public IPoeTradesListViewModel TradesList { get; }

        public IRecheckPeriodViewModel RecheckPeriod { get; }

        public bool AudioNotificationEnabled
        {
            get { return audioNotificationEnabled; }
            set { this.RaiseAndSetIfChanged(ref audioNotificationEnabled, value); }
        }

        public PoeQueryViewModel Query { get; }

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

            TradesList.TradesList.Clear();
            TradesList.ActiveQuery = query;
            RebuildTabName();
            Query.IsExpanded = false;

            if (TradesList.RecheckPeriod == TimeSpan.Zero)
            {
                Log.Instance.Debug($"[MainWindowTabViewModel.SearchCommandExecute] Auto-recheck is disabled, refreshing query manually...");
                TradesList.Refresh();
            }
        }

        private void MarkAllAsReadExecute(object arg)
        {
            using (TradesList.TradesList.SuppressChangeNotifications())
            {
                foreach (var trade in TradesList.TradesList.ToArray())
                {
                    trade.TradeState = PoeTradeState.Normal;
                }
            }
        }
    }
}