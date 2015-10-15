﻿namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Threading;

    using DumpToText;

    using Factory;

    using Guards;

    using JetBrains.Annotations;

    using Models;

    using PoeShared;
    using PoeShared.Common;
    using PoeShared.PoeTrade;
    using PoeShared.PoeTrade.Query;
    using PoeShared.Utilities;

    using ReactiveUI;

    using TypeConverter;

    using Utilities;

    internal sealed class PoeTradesListViewModel : DisposableReactiveObject
    {
        private static readonly TimeSpan TimeSinceLastUpdateRefreshTimeout = TimeSpan.FromSeconds(1);

        private readonly IClock clock;
        private readonly IEqualityComparer<IPoeItem> poeItemsComparer;
        private readonly IFactory<IPoeTradeViewModel, IPoeItem> poeTradeViewModelFactory;

        private IPoeLiveHistoryProvider activeHistoryProvider;
        private readonly SerialDisposable activeHistoryProviderDisposable = new SerialDisposable();

        private DateTime lastUpdateTimestamp;
        private IPoeQueryInfo queryInfo;
        private TimeSpan recheckTimeout = TimeSpan.FromSeconds(60);
        private readonly ReactiveList<IPoeTradeViewModel> tradesList = new ReactiveList<IPoeTradeViewModel>() {ChangeTrackingEnabled = true};

        public PoeTradesListViewModel(
            [NotNull] IFactory<IPoeLiveHistoryProvider, IPoeQuery> poeLiveHistoryFactory,
            [NotNull] IFactory<IPoeTradeViewModel, IPoeItem> poeTradeViewModelFactory,
            [NotNull] IEqualityComparer<IPoeItem> poeItemsComparer,
            [NotNull] IConverter<IPoeQueryInfo, IPoeQuery> poeQueryInfoToQueryConverter,
            [NotNull] IClock clock)
        {
            Guard.ArgumentNotNull(() => poeLiveHistoryFactory);
            Guard.ArgumentNotNull(() => poeTradeViewModelFactory);
            Guard.ArgumentNotNull(() => poeQueryInfoToQueryConverter);
            Guard.ArgumentNotNull(() => poeItemsComparer);
            Guard.ArgumentNotNull(() => clock);

            this.poeTradeViewModelFactory = poeTradeViewModelFactory;
            this.poeItemsComparer = poeItemsComparer;
            this.clock = clock;

            Anchors.Add(activeHistoryProviderDisposable);

            this.WhenAnyValue(x => x.QueryInfo)
                                     .DistinctUntilChanged()
                                     .Do(_ => lastUpdateTimestamp = clock.CurrentTime)
                                     .Where(x => x != null)
                                     .Select(poeQueryInfoToQueryConverter.Convert)
                                     .Select(poeLiveHistoryFactory.Create)
                                     .Do(OnNextHistoryProviderCreated)
                                     .Select(x => x.ItemsPacks)
                                     .Switch()
                                     .ObserveOn(Dispatcher.CurrentDispatcher)
                                     .Subscribe(OnNextItemsPackReceived)
                                     .AddTo(Anchors);

            Observable
                .Timer(DateTimeOffset.Now, TimeSinceLastUpdateRefreshTimeout)
                .Subscribe(_ => this.RaisePropertyChanged(nameof(TimeSinceLastUpdate)))
                .AddTo(Anchors);
        }

        public ReactiveList<IPoeTradeViewModel> TradesList => tradesList;

        public IPoeQueryInfo QueryInfo
        {
            get { return queryInfo; }
            set { this.RaiseAndSetIfChanged(ref queryInfo, value); }
        }

        private Exception lastUpdateException;

        public Exception LastUpdateException
        {
            get { return lastUpdateException; }
            set { this.RaiseAndSetIfChanged(ref lastUpdateException, value); }
        }

        public TimeSpan TimeSinceLastUpdate => clock.CurrentTime - lastUpdateTimestamp;

        public TimeSpan RecheckTimeout
        {
            get { return recheckTimeout; }
            set { this.RaiseAndSetIfChanged(ref recheckTimeout, value); }
        }

        public bool IsBusy => activeHistoryProvider?.IsBusy ?? false;

        private void OnNextItemsPackReceived(IPoeItem[] itemsPack)
        {
            var existingItems = tradesList.Select(x => x.Trade).ToArray();

            var removedItems = existingItems.Except(itemsPack, poeItemsComparer).ToArray();
            var newItems = itemsPack.Except(existingItems, poeItemsComparer).ToArray();

            foreach (
                var itemViewModel in
                    removedItems.Select(item => tradesList.Single(x => poeItemsComparer.Equals(x.Trade, item))))
            {
                itemViewModel.TradeState = PoeTradeState.Removed;
            }

            foreach (var item in newItems)
            {
                var itemViewModel = poeTradeViewModelFactory.Create(item);
                Anchors.Add(itemViewModel);
                itemViewModel.TradeState = PoeTradeState.New;

                itemViewModel
                    .WhenAnyValue(x => x.TradeState)
                    .WithPrevious((prev,curr) => new {prev,curr})
                    .Where(x => x.curr == PoeTradeState.Normal && x.prev == PoeTradeState.Removed)
                    .Subscribe(() => RemoveTrade(itemViewModel))
                    .AddTo(Anchors);

                tradesList.Add(itemViewModel);

                itemViewModel.IndexedAtTimestamp = clock.CurrentTime;
            }

            lastUpdateTimestamp = clock.CurrentTime;
        }

        private void OnNextHistoryProviderCreated(IPoeLiveHistoryProvider poeLiveHistoryProvider)
        {
            Log.Instance.Debug(
                $"[TradesListViewModel] Setting up new HistoryProvider (updateTimeout: {recheckTimeout})...");
            activeHistoryProvider = poeLiveHistoryProvider;
            activeHistoryProviderDisposable.Disposable = poeLiveHistoryProvider;

            this.WhenAnyValue(x => x.RecheckTimeout)
                .DistinctUntilChanged()
                .Subscribe(x => poeLiveHistoryProvider.RecheckPeriod = x)
                .AddTo(Anchors);

            poeLiveHistoryProvider
               .WhenAnyValue(x => x.IsBusy)
               .DistinctUntilChanged()
               .Subscribe(() => this.RaisePropertyChanged(nameof(IsBusy)))
               .AddTo(Anchors);

            poeLiveHistoryProvider
                .UpdateExceptions
                .Subscribe(OnErrorReceived)
                .AddTo(Anchors);

            poeLiveHistoryProvider.RecheckPeriod = recheckTimeout;
        }

        private void OnErrorReceived(Exception error)
        {
            if (error != null)
            {
                Log.Instance.Debug($"[TradesListViewModel] Received an exception from history provider\r\nQuery: {queryInfo?.DumpToTextValue() }");
            }
            LastUpdateException = error;
        }

        public void ClearTradesList()
        {
            tradesList.Clear();
        }

        private void RemoveTrade(IPoeTradeViewModel trade)
        {
            tradesList.Remove(trade);
        }
    }
}