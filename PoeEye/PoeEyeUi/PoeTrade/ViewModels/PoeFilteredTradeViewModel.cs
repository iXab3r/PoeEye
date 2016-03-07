namespace PoeEyeUi.PoeTrade.ViewModels
{
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

            trade.WhenAnyValue(x => x.TradeState)
                 .Subscribe(() => this.RaisePropertyChanged())
                 .AddTo(Anchors);

            owner.WhenAnyValue(x => x.AudioNotificationEnabled)
                 .Subscribe(() => this.RaisePropertyChanged())
                 .AddTo(Anchors);
        }

        public IMainWindowTabViewModel Owner { [NotNull] get; }

        public IPoeTradeViewModel Trade { [NotNull] get; }
    }
}