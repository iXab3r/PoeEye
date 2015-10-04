namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.Linq;
    using System.Windows;
    using System.Windows.Input;

    using Factory;

    using Guards;

    using JetBrains.Annotations;

    using MahApps.Metro.Controls.Dialogs;

    using Models;

    using PoeShared.Common;

    using ReactiveUI;

    internal sealed class PoeTradeViewModel : ReactiveObject, IPoeTradeViewModel
    {
        private readonly IPoeItem poeItem;
        private readonly IPoePriceCalculcator poePriceCalculcator;
        private PoeTradeState tradeState;
        private readonly ReactiveCommand<object> copyPmMessageToClipboardCommand;
        private readonly ReactiveCommand<object> markAsReadCommand;

        public PoeTradeViewModel(
            [NotNull] IPoeItem poeItem,
            [NotNull] IPoePriceCalculcator poePriceCalculcator,
            [NotNull] IFactory<ImageViewModel, Uri> imageViewModelFactory,
            [NotNull] IFactory<PoeLinksInfoViewModel, IPoeLinksInfo> linksViewModelFactory)
        {
            Guard.ArgumentNotNull(() => poeItem);
            Guard.ArgumentNotNull(() => poePriceCalculcator);
            Guard.ArgumentNotNull(() => imageViewModelFactory);
            Guard.ArgumentNotNull(() => linksViewModelFactory);


            this.poeItem = poeItem;
            this.poePriceCalculcator = poePriceCalculcator;
            copyPmMessageToClipboardCommand = ReactiveCommand.Create();
            copyPmMessageToClipboardCommand.Subscribe(CopyPmMessageToClipboardCommandExecute);

            markAsReadCommand = ReactiveCommand.Create();
            markAsReadCommand.Subscribe(MarkAsReadCommandExecute);

            Uri imageUri;
            if (!string.IsNullOrWhiteSpace(poeItem.ItemIconUri) && Uri.TryCreate(poeItem.ItemIconUri, UriKind.Absolute, out imageUri))
            {
                ImageViewModel = imageViewModelFactory.Create(imageUri);
            }

            if (poeItem.Links != null)
            {
                LinksViewModel = linksViewModelFactory.Create(poeItem.Links);
            }

            PriceInChaosOrbs = poePriceCalculcator.GetEquivalentInChaosOrbs(poeItem.Price);
        }

        public PoeTradeState TradeState
        {
            get { return tradeState; }
            set { this.RaiseAndSetIfChanged(ref tradeState, value); }
        }

        public ImageViewModel ImageViewModel { get; }

        public PoeLinksInfoViewModel LinksViewModel { get; }

        public string Name => poeItem.ItemName;

        public string UserIgn => poeItem.UserIgn;

        public string Price => poeItem.Price;

        public float? PriceInChaosOrbs { get; }

        public IPoeItemMod[] ImplicitMods => poeItem.Mods.Where(x => x.ModType == PoeModType.Implicit).ToArray();

        public IPoeItemMod[] ExplicitMods => poeItem.Mods.Where(x => x.ModType == PoeModType.Explicit).ToArray();

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