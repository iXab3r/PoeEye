namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.Windows;
    using System.Windows.Input;

    using Guards;

    using JetBrains.Annotations;

    using MahApps.Metro.Controls.Dialogs;

    using PoeShared.Common;

    using ReactiveUI;

    internal sealed class PoeTradeViewModel : ReactiveObject, IPoeTradeViewModel
    {
        private readonly IPoeItem poeItem;
        private PoeTradeState tradeState;
        private readonly ReactiveCommand<object> copyPmMessageToClipboardCommand;
        private readonly ReactiveCommand<object> markAsReadCommand;

        public PoeTradeViewModel([NotNull] IPoeItem poeItem)
        {
            Guard.ArgumentNotNull(() => poeItem);

            this.poeItem = poeItem;
            copyPmMessageToClipboardCommand = ReactiveCommand.Create();
            copyPmMessageToClipboardCommand.Subscribe(CopyPmMessageToClipboardCommandExecute);

            markAsReadCommand = ReactiveCommand.Create();
            markAsReadCommand.Subscribe(MarkAsReadCommandExecute);
        }

        public PoeTradeState TradeState
        {
            get { return tradeState; }
            set { this.RaiseAndSetIfChanged(ref tradeState, value); }
        }

        public string Name => poeItem.ItemName;

        public string UserIgn => poeItem.UserIgn;

        public string Price => poeItem.Price;

        public IPoeItemMod[] Mods => poeItem.Mods;

        public IPoeItem Trade => poeItem;

        public ICommand CopyPmMessageToClipboardCommand => copyPmMessageToClipboardCommand;

        public ICommand MarkAsReadCommand => markAsReadCommand;

        private void CopyPmMessageToClipboardCommandExecute(object arg)
        {
            var message = $"@{UserIgn} Hi, I would like to buy your {Name} listed for {Price} in {poeItem.League}";
            Clipboard.SetText(message);
        }

        private void MarkAsReadCommandExecute(object arg)
        {
            this.TradeState = PoeTradeState.Normal;
        }
    }
}