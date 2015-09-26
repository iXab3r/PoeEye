namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Windows.Threading;

    using Factory;

    using Guards;

    using JetBrains.Annotations;

    using PoeShared;
    using PoeShared.Common;
    using PoeShared.PoeTrade;
    using PoeShared.PoeTrade.Query;

    using ReactiveUI;

    internal sealed class TradesListViewModel : ReactiveObject
    {
        private readonly IFactory<IPoeTradeViewModel, IPoeItem> poeTradeViewModelFactory;
        private IPoeQuery query;

        private IPoeLiveHistoryProvider activeHistoryProvider;
        private TimeSpan recheckTimeout;
        private readonly ObservableCollection<IPoeTradeViewModel> tradesList = new ObservableCollection<IPoeTradeViewModel>();

        public TradesListViewModel(
            [NotNull] IFactory<IPoeLiveHistoryProvider, IPoeQuery> poeLiveHistoryFactory,
            [NotNull] IFactory<IPoeTradeViewModel, IPoeItem> poeTradeViewModelFactory)
        {
            Guard.ArgumentNotNull(() => poeLiveHistoryFactory);
            Guard.ArgumentNotNull(() => poeLiveHistoryFactory);

            this.poeTradeViewModelFactory = poeTradeViewModelFactory;

            this.WhenAnyValue(x => x.Query)
                .DistinctUntilChanged()
                .Where(x => x != null)
                .Select(poeLiveHistoryFactory.Create)
                .Do(OnNextHistoryProviderCreated)
                .Select(x => x.ItemsPacks)
                .Switch()
                .ObserveOn(Dispatcher.CurrentDispatcher)
                .Subscribe(OnNextItemsPackReceived);

            this.WhenAnyValue(x => x.RecheckTimeout)
                .DistinctUntilChanged()
                .Where(x => activeHistoryProvider != null)
                .Subscribe(x => activeHistoryProvider.RecheckPeriod = x);
        }

        private void OnNextItemsPackReceived(IPoeItem[] itemsPack)
        {
            var existingItems = tradesList.Select(x => x.Trade).ToArray();

            var removedItems = existingItems.Except(itemsPack).ToArray();
            var newItems = itemsPack.Except(existingItems).ToArray();

            foreach (var itemViewModel in removedItems.Select(item => tradesList.Single(x => x.Trade == item)))
            {
                itemViewModel.TradeState = PoeTradeState.Removed;
            }

            foreach (var item in newItems)
            {
                var itemViewModel = poeTradeViewModelFactory.Create(item);
                itemViewModel.TradeState = PoeTradeState.New;
                tradesList.Add(itemViewModel);
            }
        }

        private void OnNextHistoryProviderCreated(IPoeLiveHistoryProvider poeLiveHistoryProvider)
        {
            Log.Instance.Debug($"[TradesListViewModel] Setting up new HistoryProvider (updateTimeout: {recheckTimeout})...");
            activeHistoryProvider = poeLiveHistoryProvider;
            poeLiveHistoryProvider.RecheckPeriod = recheckTimeout;
        }

        public ObservableCollection<IPoeTradeViewModel> TradesList => tradesList;

        public IPoeQuery Query
        {
            get { return query; }
            set { this.RaiseAndSetIfChanged(ref query, value); }
        }

        public TimeSpan RecheckTimeout
        {
            get { return recheckTimeout; }
            set { this.RaiseAndSetIfChanged(ref recheckTimeout, value); }
        }
    }
}