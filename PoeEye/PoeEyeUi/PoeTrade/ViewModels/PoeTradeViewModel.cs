namespace PoeEyeUi.PoeTrade.ViewModels
{
    using Guards;

    using JetBrains.Annotations;

    using PoeShared.Common;

    using ReactiveUI;

    internal sealed class PoeTradeViewModel : ReactiveObject, IPoeTradeViewModel
    {
        private readonly IPoeItem poeItem;
        private PoeTradeState tradeState;

        public PoeTradeViewModel([NotNull] IPoeItem poeItem)
        {
            Guard.ArgumentNotNull(() => poeItem);
            
            this.poeItem = poeItem;
        }

        public PoeTradeState TradeState
        {
            get { return tradeState; }
            set { this.RaiseAndSetIfChanged(ref tradeState, value); }
        }

        public string Name => poeItem.ItemName;

        public string UserIgn => poeItem.UserIgn;

        public string Price => poeItem.Price;

        public IPoeItem Trade => poeItem;
    }
}