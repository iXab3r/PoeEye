namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;

    using Factory;

    using Guards;

    using JetBrains.Annotations;

    using Models;

    using PoeShared.Common;
    using PoeShared.Utilities;

    using ReactiveUI;

    internal sealed class HistoricalTradesViewModel : DisposableReactiveObject, IHistoricalTradesViewModel
    {
        private readonly IPoePriceCalculcator poePriceCalculcator;
        private readonly IReactiveList<IPoeItem> itemsList = new ReactiveList<IPoeItem>();

        private bool isExpanded;

        public HistoricalTradesViewModel([NotNull] IPoePriceCalculcator poePriceCalculcator)
        {
            Guard.ArgumentNotNull(() => poePriceCalculcator);

            this.poePriceCalculcator = poePriceCalculcator;
        }

        public bool IsExpanded
        {
            get { return isExpanded; }
            set { this.RaiseAndSetIfChanged(ref isExpanded, value); }
        }

        public IEnumerable<IPoeItem> Items => itemsList;

        public void AddItems(params IPoeItem[] items)
        {
            Guard.ArgumentNotNull(() => items);

            this.itemsList.AddRange(items);
        }

        public void Clear()
        {
            itemsList.Clear();
        }
    }
}