using System.Windows.Input;
using PoeShared.Common;

namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;

    internal interface IPoeTradeViewModel
    {
        ICommand CopyPmMessageToClipboardCommand { get; }
        IPoeItemMod[] ExplicitMods { get; }
        ImageViewModel ImageViewModel { get; }
        IPoeItemMod[] ImplicitMods { get; }
        PoeLinksInfoViewModel LinksViewModel { get; }
        ICommand MarkAsReadCommand { get; }
        string Name { get; }
        string Price { get; }
        float? PriceInChaosOrbs { get; }
        IPoeItem Trade { get; }
        string UserIgn { get; }

        DateTime IndexedAtTimestamp { get; set; }
        PoeTradeState TradeState { get; set; }
    }
}