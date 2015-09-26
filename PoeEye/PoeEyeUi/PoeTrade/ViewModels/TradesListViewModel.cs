namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Reactive.Linq;

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
        private IPoeQuery query;

        private IPoeLiveHistoryProvider activeHistoryProvider;
        private TimeSpan recheckTimeout;
        private readonly ObservableCollection<IPoeTradeViewModel> tradesList = new ObservableCollection<IPoeTradeViewModel>();

        public TradesListViewModel(
            [NotNull] IFactory<IPoeLiveHistoryProvider, IPoeQuery> poeLiveHistoryFactory)
        {
            Guard.ArgumentNotNull(() => poeLiveHistoryFactory);
            
            this.WhenAnyValue(x => x.Query)
                .DistinctUntilChanged()
                .Select(poeLiveHistoryFactory.Create)
                .Do(OnNextHistoryProviderCreated)
                .Select(x => x.ItemsPacks)
                .Switch()
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

            foreach (var itemViewModel in newItems.Select(item => tradesList.Single(x => x.Trade == item)))
            {
                itemViewModel.TradeState = PoeTradeState.New;
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