namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.Reactive.Linq;

    using Guards;

    using JetBrains.Annotations;

    using PoeShared.Scaffolding;

    using ReactiveUI;

    internal sealed class PoeFilteredTradeViewModel : DisposableReactiveObject
    {
        public PoeFilteredTradeViewModel([NotNull] IMainWindowTabViewModel owner, [NotNull] IPoeTradeViewModel trade)
        {
            Guard.ArgumentNotNull(() => owner);
            Guard.ArgumentNotNull(() => trade);

            Owner = owner;
            Trade = trade;

            Observable.Merge(
                    trade.WhenAnyValue(x => x.TradeState).ToUnit(),
                    trade.WhenAnyValue(x => x.PriceInChaosOrbs).ToUnit(),
                    owner.WhenAnyValue(x => x.AudioNotificationEnabled).ToUnit(),
                    owner.WhenAnyValue(x => x.Query).ToUnit())
               .Subscribe(() => this.RaisePropertyChanged())
               .AddTo(Anchors);
        }

        public string Description => FormatDescription(Owner.Query?.Description);

        public IMainWindowTabViewModel Owner { [NotNull] get; }

        public IPoeTradeViewModel Trade { [NotNull] get; }

        public PoeTradeState TradeState => Trade.TradeState;

        public float? PriceInChaosOrbs => Trade.PriceInChaosOrbs;

        public DateTime Timestamp => Trade.Trade.Timestamp;

        private string FormatDescription(string description)
        {
            return description?.Replace(Environment.NewLine, " / ");
        }
    }
}