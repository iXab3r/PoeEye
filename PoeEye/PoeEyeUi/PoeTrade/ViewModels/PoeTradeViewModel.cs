namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;

    using Factory;

    using Guards;

    using JetBrains.Annotations;

    using Models;

    using PoeShared;
    using PoeShared.Common;

    using ReactiveUI;

    using PoeShared.Utilities;

    internal sealed class PoeTradeViewModel : DisposableReactiveObject, IPoeTradeViewModel
    {
        private static readonly TimeSpan RefreshTimeout = TimeSpan.FromSeconds(10);

        private readonly IClock clock;
        private readonly ReactiveCommand<object> copyPmMessageToClipboardCommand = ReactiveCommand.Create();
        private readonly ReactiveCommand<object> markAsReadCommand = ReactiveCommand.Create();
        private readonly ReactiveCommand<object> openForumUriCommand;

        private DateTime indexedAtTimestamp;
        private PoeTradeState tradeState;

        public PoeTradeViewModel(
            [NotNull] IPoeItem poeItem,
            [NotNull] IPoePriceCalculcator poePriceCalculcator,
            [NotNull] IFactory<ImageViewModel, Uri> imageViewModelFactory,
            [NotNull] IFactory<PoeLinksInfoViewModel, IPoeLinksInfo> linksViewModelFactory,
            [NotNull] IClock clock)
        {
            this.clock = clock;
            Guard.ArgumentNotNull(() => poeItem);
            Guard.ArgumentNotNull(() => poePriceCalculcator);
            Guard.ArgumentNotNull(() => imageViewModelFactory);
            Guard.ArgumentNotNull(() => linksViewModelFactory);
            Guard.ArgumentNotNull(() => clock);

            this.Trade = poeItem;
            copyPmMessageToClipboardCommand.Subscribe(CopyPmMessageToClipboardCommandExecute);

            openForumUriCommand = ReactiveCommand.Create(Observable.Return(OpenForumUriCommandCanExecute()));
            openForumUriCommand.Subscribe(OpenForumUriCommandExecute).AddTo(Anchors);

            markAsReadCommand.Subscribe(MarkAsReadCommandExecute).AddTo(Anchors);

            Uri imageUri;
            if (!string.IsNullOrWhiteSpace(poeItem.ItemIconUri) && Uri.TryCreate(poeItem.ItemIconUri, UriKind.Absolute, out imageUri))
            {
                ImageViewModel = imageViewModelFactory.Create(imageUri);
                Anchors.Add(ImageViewModel);
            }

            if (poeItem.Links != null)
            {
                LinksViewModel = linksViewModelFactory.Create(poeItem.Links);
                Anchors.Add(LinksViewModel);
            }

            PriceInChaosOrbs = poePriceCalculcator.GetEquivalentInChaosOrbs(poeItem.Price);

            this.WhenAnyValue(x => x.IndexedAtTimestamp).ToUnit()
                .Merge(Observable.Timer(DateTimeOffset.Now, RefreshTimeout).ToUnit())
                .Subscribe(() => this.RaisePropertyChanged(nameof(TimeElapsedSinceLastIndexation)))
                .AddTo(Anchors);
        }

        public DateTime IndexedAtTimestamp
        {
            get { return indexedAtTimestamp; }
            set { this.RaiseAndSetIfChanged(ref indexedAtTimestamp, value); }
        }

        public TimeSpan TimeElapsedSinceLastIndexation => IndexedAtTimestamp == DateTime.MinValue ? TimeSpan.Zero : clock.CurrentTime - IndexedAtTimestamp;

        public ICommand OpenForumUriCommand => openForumUriCommand;

        public PoeTradeState TradeState
        {
            get { return tradeState; }
            set { this.RaiseAndSetIfChanged(ref tradeState, value); }
        }

        public ImageViewModel ImageViewModel { get; }

        public PoeLinksInfoViewModel LinksViewModel { get; }

        public float? PriceInChaosOrbs { get; }

        public IPoeItemMod[] ImplicitMods => Trade.Mods.Where(x => x.ModType == PoeModType.Implicit).ToArray();

        public IPoeItemMod[] ExplicitMods => Trade.Mods.Where(x => x.ModType == PoeModType.Explicit).ToArray();

        public IPoeItem Trade { get; }

        public ICommand CopyPmMessageToClipboardCommand => copyPmMessageToClipboardCommand;

        public ICommand MarkAsReadCommand => markAsReadCommand;

        private void OpenForumUriCommandExecute(object arg)
        {
            Guard.ArgumentIsTrue(() => OpenForumUriCommandCanExecute());

            Task.Run(() => OpenUri(Trade.TradeForumUri));
        }

        private bool OpenForumUriCommandCanExecute()
        {
            return !string.IsNullOrWhiteSpace(Trade.TradeForumUri);
        }

        private void CopyPmMessageToClipboardCommandExecute(object arg)
        {
            var message = $"@{Trade.UserIgn} Hi, I would like to buy your {Trade.ItemName} listed for {Trade.Price} in {Trade.League}";
            Clipboard.SetText(message);
        }

        private void MarkAsReadCommandExecute(object arg)
        {
            TradeState = PoeTradeState.Normal;
        }

        private void OpenUri(string uri)
        {
            try
            {
                Process.Start(uri);
            }
            catch (Exception ex)
            {
                Log.Instance.Warn($"Failed to open forum Uri '{uri}'", ex);
            }
        }
    }
}