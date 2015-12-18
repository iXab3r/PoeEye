namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Windows.Input;

    using Guards;

    using JetBrains.Annotations;

    using Models;

    using PoeShared;
    using PoeShared.DumpToText;
    using PoeShared.PoeTrade;
    using PoeShared.PoeTrade.Query;
    using PoeShared.Utilities;

    using ReactiveUI;

    using Utilities;

    internal sealed class MainWindowTabViewModel : DisposableReactiveObject
    {
        private static int GlobalTabIdx = 0;

        private readonly ReactiveCommand<object> markAllAsRead;
        private readonly ReactiveCommand<object> searchCommand;
        private readonly ReactiveCommand<object> refreshCommand;

        private bool audioNotificationEnabled;

        private readonly string tabHeader;
        private string tabName;

        public MainWindowTabViewModel(
            [NotNull] PoeTradesListViewModel tradesListViewModel,
            [NotNull] IAudioNotificationsManager audioNotificationsManager,
            [NotNull] IRecheckPeriodViewModel recheckPeriodViewModel,
            [NotNull] PoeQueryViewModel queryViewModel)
        {
            Guard.ArgumentNotNull(() => tradesListViewModel);
            Guard.ArgumentNotNull(() => audioNotificationsManager);
            Guard.ArgumentNotNull(() => queryViewModel);

            tabHeader = $"Tab #{GlobalTabIdx++}";

            TradesListViewModel = tradesListViewModel;
            RecheckPeriodViewModel = recheckPeriodViewModel;

            searchCommand = ReactiveCommand.Create();
            searchCommand.Subscribe(SearchCommandExecute);

            markAllAsRead = ReactiveCommand.Create();
            markAllAsRead.Subscribe(MarkAllAsReadExecute);

            refreshCommand = ReactiveCommand.Create(TradesListViewModel.WhenAnyValue(x => x.IsBusy).Select(x => !x));
            refreshCommand.Subscribe(RefreshCommandExecuted);

            tradesListViewModel
                .WhenAnyValue(x => x.IsBusy)
                .Subscribe(() => this.RaisePropertyChanged(nameof(IsBusy)))
                .AddTo(Anchors);

            QueryViewModel = queryViewModel;

            TradesListViewModel.TradesList.ItemChanged.ToUnit()
                               .Merge(TradesListViewModel.TradesList.Changed.ToUnit())
                               .Subscribe(() =>
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
                .Subscribe(x => audioNotificationsManager.PlayNotificationCommand.Execute(AudioNotificationType.NewItem), Log.HandleException)
                .AddTo(Anchors);

            QueryViewModel.ObservableForProperty(x => x.PoeQueryBuilder)
                          .ToUnit()
                          .StartWith(Unit.Default)
                          .Subscribe(RebuildTabName)
                          .AddTo(Anchors);

            RecheckPeriodViewModel
                .WhenAny(x => x.RecheckValue, x => x.IsAutoRecheckEnabled, (x, y) => Unit.Default)
                .Subscribe(x => TradesListViewModel.RecheckPeriod = RecheckPeriodViewModel.IsAutoRecheckEnabled ? RecheckPeriodViewModel.RecheckValue : TimeSpan.Zero)
                .AddTo(Anchors);
        }

        public PoeTradesListViewModel TradesListViewModel { get; }

        public IRecheckPeriodViewModel RecheckPeriodViewModel { get; }

        public ICommand SearchCommand => searchCommand;

        public ICommand RefreshCommand => refreshCommand;

        public ICommand MarkAllAsRead => markAllAsRead;

        public bool IsBusy => TradesListViewModel.IsBusy;

        public bool AudioNotificationEnabled
        {
            get { return audioNotificationEnabled; }
            set { this.RaiseAndSetIfChanged(ref audioNotificationEnabled, value); }
        }

        public string TabName
        {
            get { return tabName; }
            private set { this.RaiseAndSetIfChanged(ref tabName, value); }
        }

        public bool HasNewTrades => NewItemsCount > 0;

        public int NewItemsCount
        {
            get { return TradesListViewModel.TradesList.Count(x => x.TradeState == PoeTradeState.New); }
        }

        public int RemovedItemsCount
        {
            get { return TradesListViewModel.TradesList.Count(x => x.TradeState == PoeTradeState.Removed); }
        }

        public int NormalItemsCount
        {
            get { return TradesListViewModel.TradesList.Count(x => x.TradeState == PoeTradeState.Normal); }
        }

        public PoeQueryViewModel QueryViewModel { get; }

        private void RebuildTabName()
        {
            Log.Instance.Debug($"[MainWindowTabViewModel.RebuildTabName] Rebuilding tab name, tabQueryMode: {QueryViewModel}...");
            var queryDescription = QueryViewModel.Description;
            TabName = string.IsNullOrWhiteSpace(queryDescription)
                ? tabHeader
                : $"{queryDescription}";
        }

        private void RefreshCommandExecuted(object arg)
        {
            TradesListViewModel.Refresh();
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

            TradesListViewModel.TradesList.Clear();
            TradesListViewModel.ActiveQuery = query;
            RebuildTabName();
            QueryViewModel.IsExpanded = false;

            if (TradesListViewModel.RecheckPeriod == TimeSpan.Zero)
            {
                Log.Instance.Debug($"[MainWindowTabViewModel.SearchCommandExecute] Auto-recheck is disabled, refreshing query manually...");
                TradesListViewModel.Refresh();
            }
        }

        private void MarkAllAsReadExecute(object arg)
        {
            using (TradesListViewModel.TradesList.SuppressChangeNotifications())
            {
                foreach (var trade in TradesListViewModel.TradesList.ToArray())
                {
                    trade.TradeState = PoeTradeState.Normal;
                }
            }
        }
    }
}