﻿namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Concurrency;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;

    using Guards;

    using JetBrains.Annotations;

    using Microsoft.Practices.Unity;

    using Models;

    using PoeShared;
    using PoeShared.Common;
    using PoeShared.Exceptions;
    using PoeShared.PoeTrade;
    using PoeShared.PoeTrade.Query;
    using PoeShared.Prism;
    using PoeShared.Scaffolding;

    using Prism;

    using ReactiveUI;

    using TypeConverter;

    internal sealed class PoeTradesListViewModel : DisposableReactiveObject, IPoeTradesListViewModel
    {
        private static readonly TimeSpan TimeSinceLastUpdateRefreshTimeout = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan RecheckPeriodThrottleTimeout = TimeSpan.FromSeconds(1);

        private readonly SerialDisposable activeHistoryProviderDisposable = new SerialDisposable();
        private readonly IPoeCaptchaRegistrator captchaRegistrator;

        private readonly IClock clock;
        private readonly IEqualityComparer<IPoeItem> poeItemsComparer;
        private readonly IFactory<IPoeTradeViewModel, IPoeItem> poeTradeViewModelFactory;
        private readonly IScheduler uiScheduler;

        private ActiveProviderInfo activeProviderInfo;
        private IPoeQueryInfo activeQuery;

        private string errors;

        private DateTime lastUpdateTimestamp;

        private TimeSpan recheckPeriod;

        public PoeTradesListViewModel(
            [NotNull] IFactory<IPoeLiveHistoryProvider, IPoeQuery> poeLiveHistoryFactory,
            [NotNull] IFactory<IPoeTradeViewModel, IPoeItem> poeTradeViewModelFactory,
            [NotNull] IPoeCaptchaRegistrator captchaRegistrator,
            [NotNull] IHistoricalTradesViewModel historicalTradesViewModel,
            [NotNull] IEqualityComparer<IPoeItem> poeItemsComparer,
            [NotNull] IConverter<IPoeQueryInfo, IPoeQuery> poeQueryInfoToQueryConverter,
            [NotNull] IClock clock,
            [NotNull] [Dependency(WellKnownSchedulers.Ui)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(() => poeLiveHistoryFactory);
            Guard.ArgumentNotNull(() => poeTradeViewModelFactory);
            Guard.ArgumentNotNull(() => captchaRegistrator);
            Guard.ArgumentNotNull(() => historicalTradesViewModel);
            Guard.ArgumentNotNull(() => poeQueryInfoToQueryConverter);
            Guard.ArgumentNotNull(() => poeItemsComparer);
            Guard.ArgumentNotNull(() => clock);
            Guard.ArgumentNotNull(() => uiScheduler);

            this.poeTradeViewModelFactory = poeTradeViewModelFactory;
            this.poeItemsComparer = poeItemsComparer;
            this.uiScheduler = uiScheduler;
            this.clock = clock;
            this.captchaRegistrator = captchaRegistrator;

            HistoricalTradesViewModel = historicalTradesViewModel;

            Anchors.Add(activeHistoryProviderDisposable);

            this.WhenAnyValue(x => x.ActiveQuery)
                .DistinctUntilChanged()
                .WithPrevious((prev, curr) => new {prev, curr})
                .Do(
                    prevcurr =>
                    {
                        if (prevcurr.prev != null && prevcurr.curr != null)
                        {
                            HistoricalTradesViewModel.Clear();
                        }
                    })
                .Select(x => x.curr)
                .Where(x => x != null)
                .Do(_ => lastUpdateTimestamp = clock.CurrentTime)
                .Select(poeQueryInfoToQueryConverter.Convert)
                .Select(poeLiveHistoryFactory.Create)
                .Do(OnNextHistoryProviderCreated)
                .Select(x => x.ItemsPacks)
                .Switch()
                .ObserveOn(uiScheduler)
                .Subscribe(OnNextItemsPackReceived, Log.HandleException)
                .AddTo(Anchors);

            Observable
                .Timer(DateTimeOffset.Now, TimeSinceLastUpdateRefreshTimeout)
                .ObserveOn(uiScheduler)
                .Subscribe(_ => this.RaisePropertyChanged(nameof(TimeSinceLastUpdate)))
                .AddTo(Anchors);
        }

        public TimeSpan RecheckPeriod
        {
            get { return recheckPeriod; }
            set { this.RaiseAndSetIfChanged(ref recheckPeriod, value); }
        }

        public IReactiveList<IPoeTradeViewModel> TradesList { get; } = new ReactiveList<IPoeTradeViewModel> {ChangeTrackingEnabled = true};

        public IHistoricalTradesViewModel HistoricalTradesViewModel { get; }

        public IPoeQueryInfo ActiveQuery
        {
            get { return activeQuery; }
            set { this.RaiseAndSetIfChanged(ref activeQuery, value); }
        }

        public string Errors
        {
            get { return errors; }
            private set { this.RaiseAndSetIfChanged(ref errors, value); }
        }

        public TimeSpan TimeSinceLastUpdate => clock.CurrentTime - lastUpdateTimestamp;

        public bool IsBusy => activeProviderInfo.HistoryProvider?.IsBusy ?? false;

        public void Refresh()
        {
            var activeProvider = activeProviderInfo;
            activeProvider.HistoryProvider?.Refresh();
        }

        private void OnNextItemsPackReceived(IPoeItem[] itemsPack)
        {
            var activeProvider = activeProviderInfo;
            if (activeProvider.HistoryProvider == null)
            {
                return;
            }

            var existingItems = TradesList.Select(x => x.Trade).ToArray();
            var removedItems = existingItems.Except(itemsPack, poeItemsComparer).ToArray();
            var newItems = itemsPack.Except(existingItems, poeItemsComparer).ToArray();

            Log.Instance.Debug(
                $"[TradesListViewModel] Next items pack received, existingItems: {existingItems.Length}, newItems: {newItems.Length}, removedItems: {removedItems.Length}");

            foreach (var itemViewModel in TradesList.Where(x => removedItems.Contains(x.Trade)).Where(x => x.TradeState != PoeTradeState.Removed))
            {
                itemViewModel.TradeState = PoeTradeState.Removed;
                itemViewModel.Trade.Timestamp = clock.CurrentTime;
                HistoricalTradesViewModel.AddItems(itemViewModel.Trade);
            }

            if (newItems.Any())
            {
                using (TradesList.SuppressChangeNotifications())
                {
                    foreach (var item in newItems)
                    {
                        var itemViewModel = poeTradeViewModelFactory.Create(item);
                        itemViewModel.AddTo(activeProvider.Anchors);

                        itemViewModel.TradeState = PoeTradeState.New;
                        itemViewModel.Trade.Timestamp = clock.CurrentTime;

                        itemViewModel
                            .WhenAnyValue(x => x.TradeState)
                            .WithPrevious((prev, curr) => new {prev, curr})
                            .Where(x => x.curr == PoeTradeState.Normal && x.prev == PoeTradeState.Removed)
                            .Subscribe(() => RemoveItem(itemViewModel))
                            .AddTo(activeProvider.Anchors);

                        TradesList.Add(itemViewModel);
                    }
                }
            }
            lastUpdateTimestamp = clock.CurrentTime;
        }

        private void RemoveItem(IPoeTradeViewModel tradeViewModel)
        {
            TradesList.Remove(tradeViewModel);
        }

        private void OnNextHistoryProviderCreated(IPoeLiveHistoryProvider poeLiveHistoryProvider)
        {
            Log.Instance.Debug($"[TradesListViewModel] Setting up new HistoryProvider (updateTimeout: {recheckPeriod})...");

            activeProviderInfo = new ActiveProviderInfo(poeLiveHistoryProvider);
            activeHistoryProviderDisposable.Disposable = activeProviderInfo;

            poeLiveHistoryProvider
                .WhenAnyValue(x => x.IsBusy)
                .DistinctUntilChanged()
                .ObserveOn(uiScheduler)
                .Subscribe(() => this.RaisePropertyChanged(nameof(IsBusy)))
                .AddTo(activeProviderInfo.Anchors);

            poeLiveHistoryProvider
                .UpdateExceptions
                .ObserveOn(uiScheduler)
                .Subscribe(OnErrorReceived)
                .AddTo(activeProviderInfo.Anchors);

            this.WhenAnyValue(x => x.RecheckPeriod)
                .ObserveOn(uiScheduler)
                .Throttle(RecheckPeriodThrottleTimeout)
                .Subscribe(x => poeLiveHistoryProvider.RecheckPeriod = recheckPeriod)
                .AddTo(activeProviderInfo.Anchors);
        }

        private void OnErrorReceived(Exception exception)
        {
            if (exception != null)
            {
                Log.Instance.Debug($"[TradesListViewModel] Received an exception from history provider", exception);
                var errorMsg = $"[{clock.CurrentTime}] {exception.Message}";

                if (errors?.Length > 1024)
                {
                    errors = string.Empty;
                }

                Errors = string.IsNullOrEmpty(errors) ? $"{errorMsg}" : $"{errorMsg}\r\n{errors}";

                if (exception is CaptchaException)
                {
                    var captchaException = (CaptchaException) exception;
                    captchaRegistrator.CaptchaRequests.OnNext(captchaException.ResolutionUri);
                }
            }
            else
            {
                Errors = string.Empty;
            }
        }

        private struct ActiveProviderInfo : IDisposable
        {
            public CompositeDisposable Anchors { get; }

            public IPoeLiveHistoryProvider HistoryProvider { get; }

            public ActiveProviderInfo(IPoeLiveHistoryProvider provider)
            {
                HistoryProvider = provider;

                Anchors = new CompositeDisposable {HistoryProvider};
            }

            public void Dispose()
            {
                Anchors.Dispose();
            }
        }
    }
}