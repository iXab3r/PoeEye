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

    using PoeShared.PoeTrade.Query;

    using ReactiveUI;

    internal sealed class MainWindowTabViewModel : ReactiveObject
    {
        private static int tabIdx;
        private readonly ReactiveCommand<object> markAllAsRead;
        private readonly ReactiveCommand<object> searchCommand;

        private bool audioNotificationEnabled;

        private string tabName;

        public MainWindowTabViewModel(
            [NotNull] PoeTradesListViewModel tradesListViewModel,
            [NotNull] IAudioNotificationsManager audioNotificationsManager,
            [NotNull] PoeQueryViewModel queryViewModel)
        {
            Guard.ArgumentNotNull(() => tradesListViewModel);
            Guard.ArgumentNotNull(() => audioNotificationsManager);
            Guard.ArgumentNotNull(() => queryViewModel);

            tabIdx++;

            TradesListViewModel = tradesListViewModel;
            searchCommand = ReactiveCommand.Create();
            searchCommand.Subscribe(SearchCommandExecute);

            markAllAsRead = ReactiveCommand.Create();
            markAllAsRead.Subscribe(MarkAllAsReadExecute);

            tradesListViewModel
                .WhenAnyValue(x => x.LastUpdateTimestamp)
                .Subscribe(_ => this.RaisePropertyChanged(nameof(LastUpdateTimestamp)));

            tradesListViewModel
                .WhenAnyValue(x => x.RecheckTimeout)
                .Subscribe(_ => this.RaisePropertyChanged(nameof(RecheckTimeoutInSeconds)));

            tradesListViewModel
                .WhenAnyValue(x => x.IsBusy)
                .Subscribe(_ => this.RaisePropertyChanged(nameof(IsBusy)));

            QueryViewModel = queryViewModel;

            TradesListViewModel.TradesList.ItemChanged.Select(x => Unit.Default)
                               .Merge(TradesListViewModel.TradesList.Changed.Select(x => Unit.Default))
                               .Subscribe(_ =>
                                          {
                                              this.RaisePropertyChanged(nameof(NewItemsCount));
                                              this.RaisePropertyChanged(nameof(RemovedItemsCount));
                                              this.RaisePropertyChanged(nameof(NormalItemsCount));
                                              this.RaisePropertyChanged(nameof(HasNewTrades));
                                          });

            TradesListViewModel
                .WhenAnyValue(x => x.Query)
                .Subscribe(_ => RebuildTabName());

            this.WhenAnyValue(x => x.HasNewTrades)
                .DistinctUntilChanged()
                .Where(x => x && audioNotificationEnabled)
                .Subscribe(_ => audioNotificationsManager.PlayNotificationCommand.Execute(null));
        }

        public double RecheckTimeoutInSeconds
        {
            get { return TradesListViewModel.RecheckTimeout.TotalSeconds; }
            set { TradesListViewModel.RecheckTimeout = TimeSpan.FromSeconds(value); }
        }

        public PoeTradesListViewModel TradesListViewModel { get; }

        public ICommand SearchCommand => searchCommand;

        public ICommand MarkAllAsRead => markAllAsRead;

        public DateTime LastUpdateTimestamp => TradesListViewModel.LastUpdateTimestamp;

        public bool IsBusy => TradesListViewModel.IsBusy;

        public bool AudioNotificationEnabled
        {
            get { return audioNotificationEnabled; }
            set { this.RaiseAndSetIfChanged(ref audioNotificationEnabled, value); }
        }

        public string TabName
        {
            get { return tabName; }
            set { this.RaiseAndSetIfChanged(ref tabName, value); }
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
            var queryDescription = QueryViewModel.FormatQueryDescription();
            TabName = string.IsNullOrWhiteSpace(queryDescription)
                ? $"Tab #{tabIdx}"
                : $"Tab #{tabIdx}:\r\n{queryDescription}";
        }

        private void SearchCommandExecute(object arg)
        {
            var queryBuilder = arg as Func<IPoeQuery>;
            if (queryBuilder == null)
            {
                return;
            }
            var query = queryBuilder();

            TradesListViewModel.ClearTradesList();
            TradesListViewModel.Query = query;
            QueryViewModel.IsExpanded = false;
        }

        private void MarkAllAsReadExecute(object arg)
        {
            foreach (var trade in TradesListViewModel.TradesList)
            {
                trade.MarkAsReadCommand.Execute(null);
            }
        }
    }
}