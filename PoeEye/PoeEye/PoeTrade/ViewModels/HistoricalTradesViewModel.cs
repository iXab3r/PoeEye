﻿namespace PoeEye.PoeTrade.ViewModels
{
    using System.Linq;

    using Guards;

    using JetBrains.Annotations;

    using PoeShared.Common;
    using PoeShared.Prism;
    using PoeShared.Scaffolding;

    using ReactiveUI;

    internal sealed class HistoricalTradesViewModel : DisposableReactiveObject, IHistoricalTradesViewModel
    {
        private readonly IFactory<IPoeTradeViewModel, IPoeItem> poeTradeViewModelFactory;

        private bool isExpanded;

        public HistoricalTradesViewModel([NotNull] IFactory<IPoeTradeViewModel, IPoeItem> poeTradeViewModelFactory)
        {
            Guard.ArgumentNotNull(poeTradeViewModelFactory, nameof(poeTradeViewModelFactory));

            this.poeTradeViewModelFactory = poeTradeViewModelFactory;
        }

        public bool IsExpanded
        {
            get { return isExpanded; }
            set { this.RaiseAndSetIfChanged(ref isExpanded, value); }
        }

        public IReactiveList<IPoeTradeViewModel> ItemsViewModels { get; } = new ReactiveList<IPoeTradeViewModel>();

        public void AddItems(params IPoeItem[] items)
        {
            Guard.ArgumentNotNull(items, nameof(items));

            var viewModels = items.Select(poeTradeViewModelFactory.Create).ToArray();
            ItemsViewModels.AddRange(viewModels);
        }

        public void Clear()
        {
            ItemsViewModels.Clear();
        }
    }
}
