﻿namespace PoeEyeUi.PoeTrade.ViewModels
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
    using PoeShared.Utilities;

    using ReactiveUI;

    using Utilities;

    internal sealed class MainWindowTabViewModel : DisposableReactiveObject
    {
        private static int GlobalTabIdx = 0;

        private readonly ReactiveCommand<object> markAllAsRead;
        private readonly ReactiveCommand<object> searchCommand;

        private bool audioNotificationEnabled;

        private readonly string tabHeader;
        private string tabName;

        public MainWindowTabViewModel(
            [NotNull] PoeTradesListViewModel tradesListViewModel,
            [NotNull] IAudioNotificationsManager audioNotificationsManager,
            [NotNull] PoeQueryViewModel queryViewModel)
        {
            Guard.ArgumentNotNull(() => tradesListViewModel);
            Guard.ArgumentNotNull(() => audioNotificationsManager);
            Guard.ArgumentNotNull(() => queryViewModel);

            tabHeader = $"Tab #{GlobalTabIdx++}";

            TradesListViewModel = tradesListViewModel;
            searchCommand = ReactiveCommand.Create();
            searchCommand.Subscribe(SearchCommandExecute);

            markAllAsRead = ReactiveCommand.Create();
            markAllAsRead.Subscribe(MarkAllAsReadExecute);

            tradesListViewModel
                .WhenAnyValue(x => x.RecheckTimeout)
                .Subscribe(() => this.RaisePropertyChanged(nameof(RecheckTimeoutInSeconds)));

            tradesListViewModel
                .WhenAnyValue(x => x.IsBusy)
                .Subscribe(() => this.RaisePropertyChanged(nameof(IsBusy)));

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

            TradesListViewModel
                .WhenAnyValue(x => x.QueryInfo)
                .Subscribe(RebuildTabName)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.NewItemsCount)
                .DistinctUntilChanged()
                .Where(x => x > 0)
                .Where(x => audioNotificationEnabled)
                .Subscribe(() => audioNotificationsManager.PlayNotificationCommand.Execute(null))
                .AddTo(Anchors);
        }

        public double RecheckTimeoutInSeconds
        {
            get { return TradesListViewModel.RecheckTimeout.TotalSeconds; }
            set { TradesListViewModel.RecheckTimeout = TimeSpan.FromSeconds(value); }
        }

        public PoeTradesListViewModel TradesListViewModel { get; }

        public ICommand SearchCommand => searchCommand;

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
            var queryDescription = QueryViewModel.FormatQueryDescription();
            TabName = string.IsNullOrWhiteSpace(queryDescription)
                ? tabHeader
                : $"{queryDescription}";
        }

        private void SearchCommandExecute(object arg)
        {
            var queryBuilder = arg as Func<IPoeQueryInfo>;
            if (queryBuilder == null)
            {
                return;
            }
            var query = queryBuilder();

            TradesListViewModel.ClearTradesList();
            TradesListViewModel.QueryInfo = query;
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