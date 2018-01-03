using PoeEye.PoeTrade.Common;
using PoeShared.UI.ViewModels;

namespace PoeEye.PoeTrade.ViewModels
{
    using System;
    using System.Windows.Input;

    using JetBrains.Annotations;

    using PoeShared.Common;
    using PoeShared.Scaffolding;

    internal interface IPoeTradeViewModel : IDisposableReactiveObject
    {
        ICommand CopyPrivateMessageToClipboardCommand { [NotNull] get; }

        IImageViewModel Image { [NotNull] get; }

        PoeLinksInfoViewModel Links { [NotNull] get; }

        PoePrice? PriceInChaosOrbs { get; }

        TimeSpan TimeElapsedSinceLastIndexation { get; }

        IPoeItem Trade { [NotNull] get; }

        PoeTradeState TradeState { get; set; }
    }
}