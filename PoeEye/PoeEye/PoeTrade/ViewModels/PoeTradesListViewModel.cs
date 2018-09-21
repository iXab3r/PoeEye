using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Common.Logging;
using DynamicData;
using Guards;
using JetBrains.Annotations;
using LinqKit;
using PoeEye.Config;
using PoeEye.PoeTrade.Models;
using PoeShared;
using PoeShared.Common;
using PoeShared.Exceptions;
using PoeShared.Modularity;
using PoeShared.PoeTrade;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using ReactiveUI;
using Unity.Attributes;

namespace PoeEye.PoeTrade.ViewModels
{
    internal sealed class PoeTradesListViewModel : DisposableReactiveObject, IPoeTradesListViewModel
    {
        private static readonly ILog Log = LogManager.GetLogger<PoeTradesListViewModel>();
        
        private static readonly TimeSpan TimeSinceLastUpdateRefreshTimeout = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan RecheckPeriodThrottleTimeout = TimeSpan.FromSeconds(1);

        private readonly SourceCache<IPoeTradeViewModel, IPoeItem> itemsSource = new SourceCache<IPoeTradeViewModel, IPoeItem>(x => x.Trade);

        private readonly SerialDisposable activeHistoryProviderDisposable = new SerialDisposable();
        private readonly IPoeCaptchaRegistrator captchaRegistrator;

        private readonly IClock clock;
        private readonly IPoeApiWrapper poeApiWrapper;
        private readonly IEqualityComparer<IPoeItem> poeItemsComparer;
        private readonly IFactory<IPoeLiveHistoryProvider, IPoeApiWrapper, IPoeQueryInfo> poeLiveHistoryFactory;
        private readonly IFactory<IPoeTradeViewModel, IPoeItem> poeTradeViewModelFactory;
        private readonly IPoeTradeQuickFilter quickFilter;
        private readonly IScheduler uiScheduler;
        private ActiveProviderInfo activeProviderInfo;
        private IPoeQueryInfo activeQuery;

        private string errors;

        private DateTime lastUpdateTimestamp;
        private string quickFilterText;

        private TimeSpan recheckPeriod;

        public PoeTradesListViewModel(
            [NotNull] IPoeApiWrapper poeApiWrapper,
            [NotNull] IFactory<IPoeLiveHistoryProvider, IPoeApiWrapper, IPoeQueryInfo> poeLiveHistoryFactory,
            [NotNull] IFactory<IPoeTradeViewModel, IPoeItem> poeTradeViewModelFactory,
            [NotNull] IFactory<IPoeAdvancedTradesListViewModel> listFactory,
            [NotNull] IPoeCaptchaRegistrator captchaRegistrator,
            [NotNull] IEqualityComparer<IPoeItem> poeItemsComparer,
            [NotNull] IFactory<IPoeTradeQuickFilter> quickFilterFactory,
            [NotNull] IConfigProvider<PoeEyeMainConfig> configProvider,
            [NotNull] IClock clock,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(poeApiWrapper, nameof(poeApiWrapper));
            Guard.ArgumentNotNull(poeLiveHistoryFactory, nameof(poeLiveHistoryFactory));
            Guard.ArgumentNotNull(poeTradeViewModelFactory, nameof(poeTradeViewModelFactory));
            Guard.ArgumentNotNull(listFactory, nameof(listFactory));
            Guard.ArgumentNotNull(configProvider, nameof(configProvider));
            Guard.ArgumentNotNull(captchaRegistrator, nameof(captchaRegistrator));
            Guard.ArgumentNotNull(poeItemsComparer, nameof(poeItemsComparer));
            Guard.ArgumentNotNull(quickFilterFactory, nameof(quickFilterFactory));
            Guard.ArgumentNotNull(clock, nameof(clock));
            Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));

            this.poeApiWrapper = poeApiWrapper;
            this.poeLiveHistoryFactory = poeLiveHistoryFactory;
            this.poeTradeViewModelFactory = poeTradeViewModelFactory;
            this.poeItemsComparer = poeItemsComparer;
            this.uiScheduler = uiScheduler;
            this.clock = clock;
            this.captchaRegistrator = captchaRegistrator;

            Anchors.Add(activeHistoryProviderDisposable);

            this.WhenAnyValue(x => x.ActiveQuery)
                .DistinctUntilChanged()
                .WithPrevious((prev, curr) => new {prev, curr})
                .Select(x => x.curr)
                .Do(_ => lastUpdateTimestamp = clock.Now)
                .Select(HandleNextQuery)
                .Switch()
                .Subscribe(OnNextItemsPackReceived, Log.HandleException)
                .AddTo(Anchors);

            Observable
                .Timer(DateTimeOffset.Now, TimeSinceLastUpdateRefreshTimeout)
                .ObserveOn(uiScheduler)
                .Subscribe(_ => this.RaisePropertyChanged(nameof(TimeSinceLastUpdate)))
                .AddTo(Anchors);

            itemsSource.Connect()
                       .DisposeMany()
                       .Bind(out var items)
                       .Subscribe()
                       .AddTo(Anchors);

            var list = listFactory.Create().AddTo(Anchors);
            list.SortBy(nameof(IPoeTradeViewModel.TradeState), ListSortDirection.Ascending);
            list.ThenSortBy(nameof(IPoeTradeViewModel.PriceInChaosOrbs), ListSortDirection.Ascending);
            configProvider.WhenChanged.Subscribe(x => list.PageParameter.PageSize = x.ItemPageSize).AddTo(Anchors);
            list.Add(items);

            quickFilter = quickFilterFactory.Create();
            list.Filter(this.WhenAnyValue(x => x.QuickFilter).Select(x => BuildFilter()));

            Items = list.RawItems;
            ItemsView = list.Items;
            PageParameters = list.PageParameter;
        }

        public TimeSpan RecheckPeriod
        {
            get => recheckPeriod;
            set => this.RaiseAndSetIfChanged(ref recheckPeriod, value);
        }

        public ReadOnlyObservableCollection<IPoeTradeViewModel> ItemsView { get; }
        public IPageParameterDataViewModel PageParameters { get; }

        public ReadOnlyObservableCollection<IPoeTradeViewModel> Items { get; }

        public IPoeQueryInfo ActiveQuery
        {
            get => activeQuery;
            set => this.RaiseAndSetIfChanged(ref activeQuery, value);
        }

        public string Errors
        {
            get => errors;
            private set => this.RaiseAndSetIfChanged(ref errors, value);
        }

        public string QuickFilter
        {
            get => quickFilterText;
            set => this.RaiseAndSetIfChanged(ref quickFilterText, value);
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

        private Predicate<IPoeTradeViewModel> BuildFilter()
        {
            var filter = PredicateBuilder.True<IPoeTradeViewModel>();

            if (!string.IsNullOrWhiteSpace(QuickFilter))
            {
                filter = filter.And(x => quickFilter.Apply(quickFilterText, x));
            }

            return new Predicate<IPoeTradeViewModel>(filter.Compile());
        }

        private void OnNextItemsPackReceived(IPoeItem[] itemsPack)
        {
            var activeProvider = activeProviderInfo;
            if (activeProvider.HistoryProvider == null)
            {
                return;
            }

            var removedItems = itemsPack.Where(x => x.ItemState == PoeTradeState.Removed).ToArray();
            var newItems = itemsPack.Where(x => x.ItemState == PoeTradeState.New).ToArray();

            Log.Debug(
                $"[TradesListViewModel] Next items pack received, existingItems: {itemsSource.Count}, newItems: {newItems.Length}, removedItems: {removedItems.Length}");

            foreach (var item in removedItems)
            {
                Update(item, trade => trade.TradeState = PoeTradeState.Removed);
            }

            foreach (var item in newItems)
            {
                if (!itemsSource.Lookup(item).HasValue)
                {
                    var itemViewModel = poeTradeViewModelFactory.Create(item);
                    itemViewModel.AddTo(activeProvider.Anchors);

                    itemViewModel
                        .WhenAnyValue(x => x.TradeState)
                        .WithPrevious((prev, curr) => new {prev, curr})
                        .Where(x => x.curr == PoeTradeState.Normal && x.prev == PoeTradeState.Removed)
                        .Select(x => itemViewModel)
                        .Subscribe(itemsSource.Remove)
                        .AddTo(activeProvider.Anchors);

                    uiScheduler.Schedule(() =>
                    {
                        if (Log.IsTraceEnabled)
                        {
                            Log.Trace($"Adding new item: {itemViewModel.Trade.DumpToTextRaw()}");
                        }
                        itemsSource.AddOrUpdate(itemViewModel);
                    });
                }
                
                Update(item, trade =>
                {
                    trade.TradeState = PoeTradeState.New;
                    trade.Trade.Timestamp = clock.Now;
                });
            }

            lastUpdateTimestamp = clock.Now;
        }

        private void Update(IPoeItem item, Action<IPoeTradeViewModel> action)
        {
            uiScheduler.Schedule(() =>
            {
                var existing = itemsSource.Lookup(item);
                if (!existing.HasValue)
                {
                    throw new ApplicationException($"Failed to find item {item.DumpToTextRaw()}, items: \n\t{itemsSource.Items.DumpToTable()}");
                }
                //itemsSource.Remove(existing.Value);
                action(existing.Value);
                //itemsSource.AddOrUpdate(existing.Value);
            });
        }

        private IObservable<IPoeItem[]> HandleNextQuery(IPoeQueryInfo queryInfo)
        {
            if (queryInfo == null)
            {
                activeHistoryProviderDisposable.Disposable = null;
                return Observable.Never<IPoeItem[]>();
            }

            var historyProvider = poeLiveHistoryFactory.Create(poeApiWrapper, queryInfo);
            OnNextHistoryProviderCreated(historyProvider);
            return historyProvider.ItemsPacks;
        }

        private void OnNextHistoryProviderCreated(IPoeLiveHistoryProvider poeLiveHistoryProvider)
        {
            Log.Debug($"[TradesListViewModel] Setting up new HistoryProvider (updateTimeout: {recheckPeriod})...");

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
                Log.Debug($"[TradesListViewModel] Received an exception from history provider", exception);
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

                Anchors = new CompositeDisposable {HistoryProvider};
            }

            public void Dispose()
            {
                Anchors.Dispose();
            }
        }
    }
}