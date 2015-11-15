namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System.Collections.Generic;
    using System.Linq;

    using Factory;

    using Guards;

    using JetBrains.Annotations;

    using PoeShared.Common;
    using PoeShared.Utilities;

    using ReactiveUI;

    internal sealed class HistoricalTradesViewModel : DisposableReactiveObject, IHistoricalTradesViewModel
    {
        private readonly IFactory<IPoeTradeViewModel, IPoeItem> poeTradeViewModelFactory;
        private readonly IReactiveList<IPoeTradeViewModel> itemsList = new ReactiveList<IPoeTradeViewModel>();

        private bool isExpanded;

        public HistoricalTradesViewModel([NotNull] IFactory<IPoeTradeViewModel, IPoeItem> poeTradeViewModelFactory)
        {
            Guard.ArgumentNotNull(() => poeTradeViewModelFactory);

            this.poeTradeViewModelFactory = poeTradeViewModelFactory;
        }

        public bool IsExpanded
        {
            get { return isExpanded; }
            set { this.RaiseAndSetIfChanged(ref isExpanded, value); }
        }

        public IPoeItem[] Items => itemsList.Select(x => x.Trade).ToArray();

        public IEnumerable<IPoeTradeViewModel> ItemsViewModels => itemsList;

        public void AddItems(params IPoeItem[] items)
        {
            Guard.ArgumentNotNull(() => items);

            var viewModels = items.Select(poeTradeViewModelFactory.Create).ToArray();
            this.itemsList.AddRange(viewModels);
        }

        public void Clear()
        {
            itemsList.Clear();
        }
    }
}