using System.Collections.ObjectModel;
using System.Reactive.Subjects;
using DynamicData;
using DynamicData.Binding;
using PoeEye.PoeTrade.Common;

namespace PoeEye.PoeTrade.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Concurrency;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;

    using Exceptionless;

    using Guards;

    using JetBrains.Annotations;

    using Microsoft.Practices.Unity;

    using Models;

    using PoeEye.Prism;

    using PoeShared;
    using PoeShared.Common;
    using PoeShared.Exceptions;
    using PoeShared.PoeTrade;
    using PoeShared.Prism;
    using PoeShared.Scaffolding;

    using ReactiveUI;

    internal sealed class PoeTradesListViewModel : DisposableReactiveObject, IPoeTradesListViewModel
    {
        private static readonly TimeSpan TimeSinceLastUpdateRefreshTimeout = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan RecheckPeriodThrottleTimeout = TimeSpan.FromSeconds(1);

        private readonly SerialDisposable activeHistoryProviderDisposable = new SerialDisposable();
        private readonly IPoeCaptchaRegistrator captchaRegistrator;

        private readonly IClock clock;
        private readonly IEqualityComparer<IPoeItem> poeItemsComparer;
        private readonly IPoeApiWrapper poeApiWrapper;
        private readonly IFactory<IPoeLiveHistoryProvider, IPoeApiWrapper, IPoeQueryInfo> poeLiveHistoryFactory;
        private readonly IFactory<IPoeTradeViewModel, IPoeItem> poeTradeViewModelFactory;
        private readonly IScheduler uiScheduler;
        private readonly ISourceList<IPoeTradeViewModel> itemsSource = new SourceList<IPoeTradeViewModel>();
        private readonly ReadOnlyObservableCollection<IPoeTradeViewModel> items;
        
        private ActiveProviderInfo activeProviderInfo;
        private IPoeQueryInfo activeQuery;

        private string errors;

        private DateTime lastUpdateTimestamp;

        private TimeSpan recheckPeriod;

        public PoeTradesListViewModel(
            [NotNull] IPoeApiWrapper poeApiWrapper,
            [NotNull] IFactory<IPoeLiveHistoryProvider, IPoeApiWrapper, IPoeQueryInfo> poeLiveHistoryFactory,
            [NotNull] IFactory<IPoeTradeViewModel, IPoeItem> poeTradeViewModelFactory,
            [NotNull] IPoeCaptchaRegistrator captchaRegistrator,
            [NotNull] IHistoricalTradesViewModel historicalTrades,
            [NotNull] IEqualityComparer<IPoeItem> poeItemsComparer,
            [NotNull] IClock clock,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(poeApiWrapper, nameof(poeApiWrapper));
            Guard.ArgumentNotNull(poeLiveHistoryFactory, nameof(poeLiveHistoryFactory));
            Guard.ArgumentNotNull(poeTradeViewModelFactory, nameof(poeTradeViewModelFactory));
            Guard.ArgumentNotNull(captchaRegistrator, nameof(captchaRegistrator));
            Guard.ArgumentNotNull(historicalTrades, nameof(historicalTrades));
            Guard.ArgumentNotNull(poeItemsComparer, nameof(poeItemsComparer));
            Guard.ArgumentNotNull(clock, nameof(clock));
            Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));

            this.poeApiWrapper = poeApiWrapper;
            this.poeLiveHistoryFactory = poeLiveHistoryFactory;
            this.poeTradeViewModelFactory = poeTradeViewModelFactory;
            this.poeItemsComparer = poeItemsComparer;
            this.uiScheduler = uiScheduler;
            this.clock = clock;
            this.captchaRegistrator = captchaRegistrator;

            HistoricalTrades = historicalTrades;

            Anchors.Add(activeHistoryProviderDisposable);
            
            this.WhenAnyValue(x => x.ActiveQuery)
                .DistinctUntilChanged()
                .WithPrevious((prev, curr) => new { prev, curr })
                .Do(
                    prevcurr =>
                    {
                        if (prevcurr.prev != null && prevcurr.curr != null)
                        {
                            HistoricalTrades.Clear();
                        }
                    })
                .Select(x => x.curr)
                .Do(_ => lastUpdateTimestamp = clock.Now)
                .Select(HandleNextQuery)
                .Switch()
                .ObserveOn(uiScheduler)
                .Subscribe(OnNextItemsPackReceived, Log.HandleException)
                .AddTo(Anchors);

            Observable
                .Timer(DateTimeOffset.Now, TimeSinceLastUpdateRefreshTimeout)
                .ObserveOn(uiScheduler)
                .Subscribe(_ => this.RaisePropertyChanged(nameof(TimeSinceLastUpdate)))
                .AddTo(Anchors);

            itemsSource
                .Connect()
                .Sort(SortExpressionComparer<IPoeTradeViewModel>.Ascending(x => x.TradeState).ThenByAscending(x => x.PriceInChaosOrbs))
                .ObserveOn(uiScheduler)
                .Bind(out items)
                .Subscribe()
                .AddTo(Anchors);
        }

        public TimeSpan RecheckPeriod
        {
            get { return recheckPeriod; }
            set { this.RaiseAndSetIfChanged(ref recheckPeriod, value); }
        }

        public ReadOnlyObservableCollection<IPoeTradeViewModel> Items
        {
            get { return items; }
        }

        public IHistoricalTradesViewModel HistoricalTrades { get; }

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

        public TimeSpan TimeSinceLastUpdate => clock.Now - lastUpdateTimestamp;

        public bool IsBusy => activeProviderInfo.HistoryProvider?.IsBusy ?? false;

        public void Refresh()
        {
            var activeProvider = activeProviderInfo;
            activeProvider.HistoryProvider?.Refresh();
        }

        public void Clear()
        {
            itemsSource.Clear();
        }

        private void OnNextItemsPackReceived(IPoeItem[] itemsPack)
        {
            var activeProvider = activeProviderInfo;
            if (activeProvider.HistoryProvider == null)
            {
                return;
            }

            var existingItems = itemsSource.Items.Select(x => x.Trade).ToArray();
            var removedItems = existingItems.Except(itemsPack, poeItemsComparer).ToArray();
            var newItems = itemsPack.Except(existingItems, poeItemsComparer).ToArray();
            var relistedItems = itemsSource.Items
                .Where(x => x.TradeState == PoeTradeState.Removed)
                .Where(x => itemsPack.Contains(x.Trade, poeItemsComparer))
                .ToArray();

            Log.Instance.Debug(
                $"[TradesListViewModel] Next items pack received, existingItems: {existingItems.Length}, newItems: {newItems.Length}, removedItems: {removedItems.Length}, relistedItems: {relistedItems.Length}");

            foreach (var itemViewModel in relistedItems)
            {
                itemViewModel.TradeState = PoeTradeState.New;
            }

            foreach (var itemViewModel in itemsSource.Items.Where(x => removedItems.Contains(x.Trade)).Where(x => x.TradeState != PoeTradeState.Removed))
            {
                itemViewModel.TradeState = PoeTradeState.Removed;
                itemViewModel.Trade.Timestamp = clock.Now;
                HistoricalTrades.AddItems(itemViewModel.Trade);
            }

            if (newItems.Any())
            {
                foreach (var item in newItems)
                {
                    var itemViewModel = poeTradeViewModelFactory.Create(item);
                    itemViewModel.AddTo(activeProvider.Anchors);
                    itemsSource.Add(itemViewModel);

                    itemViewModel
                        .WhenAnyValue(x => x.TradeState)
                        .WithPrevious((prev, curr) => new { prev, curr })
                        .Where(x => x.curr == PoeTradeState.Normal && x.prev == PoeTradeState.Removed)
                        .Select(x => itemViewModel)
                        .Subscribe(itemsSource.Remove)
                        .AddTo(activeProvider.Anchors);

                    itemViewModel.TradeState = PoeTradeState.New;
                    itemViewModel.Trade.Timestamp = clock.Now;
                }
            }
            lastUpdateTimestamp = clock.Now;
        }

        private IObservable<IPoeItem[]> HandleNextQuery(IPoeQueryInfo queryInfo)
        {
            if (queryInfo == null)
            {
                activeHistoryProviderDisposable.Disposable = null;
                return Observable.Never<IPoeItem[]>();
            }
            else
            {
                var historyProvider = poeLiveHistoryFactory.Create(poeApiWrapper, queryInfo);
                OnNextHistoryProviderCreated(historyProvider);
                return historyProvider.ItemsPacks;
            }
        }

        private void OnNextHistoryProviderCreated(IPoeLiveHistoryProvider poeLiveHistoryProvider)
        {
            Log.Instance.Debug($"[TradesListViewModel] Setting up new HistoryProvider (updateTimeout: {recheckPeriod})...");

            activeProviderInfo = new ActiveProviderInfo(poeLiveHistoryProvider);
            activeHistoryProviderDisposable.Disposable = activeProviderInfo;

            poeLiveHistoryProvider
                .WhenAnyValue(x => x.IsBusy)
                .ObserveOn(uiScheduler)
                .Subscribe(() => this.RaisePropertyChanged(nameof(IsBusy)))
                .AddTo(activeProviderInfo.Anchors);

            poeLiveHistoryProvider
                .UpdateExceptions
                .ObserveOn(uiScheduler)
                .Subscribe(OnErrorReceived)
                .AddTo(activeProviderInfo.Anchors);

            this.WhenAnyValue(x => x.RecheckPeriod)
                .Throttle(RecheckPeriodThrottleTimeout)
                .ObserveOn(uiScheduler)
                .Subscribe(x => poeLiveHistoryProvider.RecheckPeriod = recheckPeriod)
                .AddTo(activeProviderInfo.Anchors);
        }

        private void OnErrorReceived(Exception exception)
        {
            if (exception != null)
            {
                Log.Instance.Debug($"[TradesListViewModel] Received an exception from history provider", exception);
                var errorMsg = $"[{clock.Now}] {exception.Message}";

                if (errors?.Length > 1024)
                {
                    errors = string.Empty;
                }

                Errors = string.IsNullOrEmpty(errors) ? $"{errorMsg}" : $"{errorMsg}\r\n{errors}";

                if (exception is CaptchaException)
                {
                    var captchaException = (CaptchaException)exception;
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

                Anchors = new CompositeDisposable { HistoryProvider };
            }

            public void Dispose()
            {
                Anchors.Dispose();
            }
        }
    }
}
