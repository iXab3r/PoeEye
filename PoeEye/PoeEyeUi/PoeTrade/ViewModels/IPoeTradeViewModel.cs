namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.ComponentModel;
    using System.Windows.Input;

    using JetBrains.Annotations;

    using PoeShared.Common;

    internal interface IPoeTradeViewModel : IDisposable, INotifyPropertyChanged
    {
        ICommand CopyPmMessageToClipboardCommand { [NotNull] get; }

        IPoeItemMod[] ExplicitMods { [NotNull] get; }

        ImageViewModel ImageViewModel { [NotNull] get; }

        IPoeItemMod[] ImplicitMods { [NotNull] get; }

        PoeLinksInfoViewModel LinksViewModel { [NotNull] get; }

        ICommand MarkAsReadCommand { [NotNull] get; }

        float? PriceInChaosOrbs { get; }

        IPoeItem Trade { [NotNull] get; }

        DateTime IndexedAtTimestamp { get; set; }

        PoeTradeState TradeState { get; set; }
    }
}