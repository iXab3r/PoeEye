namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Windows.Data;
    using System.Windows.Threading;

    using Factory;

    using Guards;

    using JetBrains.Annotations;

    using PoeShared;
    using PoeShared.Common;
    using PoeShared.PoeTrade;
    using PoeShared.PoeTrade.Query;

    using ReactiveUI;

    internal sealed class PoeTradesListViewModel : ReactiveObject
    {
        private readonly IClock clock;
        private readonly IEqualityComparer<IPoeItem> poeItemsComparer;
        private readonly IFactory<IPoeTradeViewModel, IPoeItem> poeTradeViewModelFactory;

        private IPoeLiveHistoryProvider activeHistoryProvider;

        private DateTime lastUpdateTimestamp;
        private IPoeQuery query;
        private TimeSpan recheckTimeout;
        private readonly ObservableCollection<IPoeTradeViewModel> tradesList = new ObservableCollection<IPoeTradeViewModel>();

        private readonly ICollectionView wrappedTradesList;

        public PoeTradesListViewModel(
            [NotNull] IFactory<IPoeLiveHistoryProvider, IPoeQuery> poeLiveHistoryFactory,
            [NotNull] IFactory<IPoeTradeViewModel, IPoeItem> poeTradeViewModelFactory,
            [NotNull] IEqualityComparer<IPoeItem> poeItemsComparer,
            [NotNull] IClock clock)
        {
            Guard.ArgumentNotNull(() => poeLiveHistoryFactory);
            Guard.ArgumentNotNull(() => poeTradeViewModelFactory);
            Guard.ArgumentNotNull(() => poeItemsComparer);
            Guard.ArgumentNotNull(() => clock);

            this.poeTradeViewModelFactory = poeTradeViewModelFactory;
            this.poeItemsComparer = poeItemsComparer;
            this.clock = clock;

            this.WhenAnyValue(x => x.Query)
                .DistinctUntilChanged()
                .Where(x => x != null)
                .Select(poeLiveHistoryFactory.Create)
                .Do(OnNextHistoryProviderCreated)
                .Select(x => x.ItemsPacks)
                .Switch()
                .ObserveOn(Dispatcher.CurrentDispatcher)
                .Subscribe(OnNextItemsPackReceived);

            wrappedTradesList = CollectionViewSource.GetDefaultView(tradesList);
            wrappedTradesList.SortDescriptions.Add(new SortDescription(nameof(IPoeTradeViewModel.TradeState), ListSortDirection.Descending));
            Guard.ArgumentIsTrue(() => wrappedTradesList.CanSort);
        }


        public ICollectionView TradesList => wrappedTradesList;

        public IPoeQuery Query
        {
            get { return query; }
            set { this.RaiseAndSetIfChanged(ref query, value); }
        }

        public DateTime LastUpdateTimestamp
        {
            get { return lastUpdateTimestamp; }
            set { this.RaiseAndSetIfChanged(ref lastUpdateTimestamp, value); }
        }

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
                itemViewModel.TradeState = PoeTradeState.New;
                tradesList.Add(itemViewModel);
            }

            LastUpdateTimestamp = clock.CurrentTime;
        }

        private void OnNextHistoryProviderCreated(IPoeLiveHistoryProvider poeLiveHistoryProvider)
        {
            Log.Instance.Debug(
                $"[TradesListViewModel] Setting up new HistoryProvider (updateTimeout: {recheckTimeout})...");
            activeHistoryProvider = poeLiveHistoryProvider;

            //TODO Memory leak
            this.WhenAnyValue(x => x.RecheckTimeout)
                .DistinctUntilChanged()
                .Subscribe(x => poeLiveHistoryProvider.RecheckPeriod = x);

            poeLiveHistoryProvider
               .WhenAnyValue(x => x.IsBusy)
               .DistinctUntilChanged()
               .Subscribe(_ => this.RaisePropertyChanged(nameof(IsBusy)));

            poeLiveHistoryProvider.RecheckPeriod = recheckTimeout;
        }

        public void ClearTradesList()
        {
            tradesList.Clear();
        }
    }
}