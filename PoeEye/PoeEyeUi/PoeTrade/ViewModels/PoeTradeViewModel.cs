namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;

    using Exceptionless;

    using Guards;

    using JetBrains.Annotations;

    using Microsoft.Practices.Unity;

    using Models;

    using PoeEye.PoeTrade;

    using PoeShared;
    using PoeShared.Common;
    using PoeShared.Prism;
    using PoeShared.Scaffolding;

    using Prism;

    using ReactiveUI;

    internal sealed class PoeTradeViewModel : DisposableReactiveObject, IPoeTradeViewModel
    {
        private static readonly TimeSpan RefreshTimeout = TimeSpan.FromSeconds(10);
        private readonly IClock clock;
        private readonly ReactiveCommand<object> copyPmMessageToClipboardCommand = ReactiveCommand.Create();

        private readonly IPoeItemVerifier itemVerifier;
        private readonly ReactiveCommand<object> openForumUriCommand;
        private readonly ReactiveCommand<object> verifyItemCommand = ReactiveCommand.Create();

        private PoeTradeState tradeState;

        private PoeItemVerificationState verificationState;

        public PoeTradeViewModel(
            [NotNull] IPoeItem poeItem,
            [NotNull] IPoePriceCalculcator poePriceCalculcator,
            [NotNull] IPoeItemVerifier itemVerifier,
            [NotNull] IFactory<ImageViewModel, Uri> imageViewModelFactory,
            [NotNull] IFactory<PoeLinksInfoViewModel, IPoeLinksInfo> linksViewModelFactory,
            [NotNull] [Dependency(WellKnownSchedulers.Ui)] IScheduler uiScheduler,
            [NotNull] IClock clock)
        {
            Guard.ArgumentNotNull(() => poeItem);
            Guard.ArgumentNotNull(() => itemVerifier);
            Guard.ArgumentNotNull(() => poePriceCalculcator);
            Guard.ArgumentNotNull(() => imageViewModelFactory);
            Guard.ArgumentNotNull(() => linksViewModelFactory);
            Guard.ArgumentNotNull(() => uiScheduler);
            Guard.ArgumentNotNull(() => clock);

            this.clock = clock;
            this.itemVerifier = itemVerifier;
            Trade = poeItem;
            copyPmMessageToClipboardCommand.Subscribe(CopyPmMessageToClipboardCommandExecute);

            openForumUriCommand = ReactiveCommand.Create(Observable.Return(OpenForumUriCommandCanExecute()));
            openForumUriCommand.Subscribe(OpenForumUriCommandExecute).AddTo(Anchors);


            verifyItemCommand.Subscribe(VerifyCommandExecuted).AddTo(Anchors);

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

            Observable.Timer(DateTimeOffset.Now, RefreshTimeout).ToUnit()
                      .ObserveOn(uiScheduler)
                      .Subscribe(() => this.RaisePropertyChanged(nameof(TimeElapsedSinceLastIndexation)))
                      .AddTo(Anchors);
        }

        public TimeSpan TimeElapsedSinceLastIndexation => Trade.Timestamp == DateTime.MinValue ? TimeSpan.Zero : clock.CurrentTime - Trade.Timestamp;

        public ICommand OpenForumUriCommand => openForumUriCommand;

        public ICommand VerifyItemCommand => verifyItemCommand;

        public PoeItemVerificationState VerificationState
        {
            get { return verificationState; }
            set { this.RaiseAndSetIfChanged(ref verificationState, value); }
        }

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
            ExceptionlessClient.Default
                .CreateFeatureUsage("TradeList")
                .SetType("CopyToClipboard")
                .SetProperty("Item", Trade.DumpToText())
                .Submit();

            string message = null;
            if (string.IsNullOrWhiteSpace(Trade.Price))
            {
                message = $"@{Trade.UserIgn} Hi, I would like to buy your {Trade.ItemName} listed in {Trade.League}, offer is ";
            }
            else
            {
                message = $"@{Trade.UserIgn} Hi, I would like to buy your {Trade.ItemName} listed for {Trade.Price} in {Trade.League}";
            }

            Clipboard.SetText(message);
        }

        private void OpenUri(string uri)
        {
            try
            {
                ExceptionlessClient.Default
                    .CreateFeatureUsage("TradeList")
                    .SetType("OpenForumUri")
                    .SetProperty("Item", Trade.DumpToText())
                    .Submit();

                Process.Start(uri);
            }
            catch (Exception ex)
            {
                Log.Instance.Warn($"Failed to open forum Uri '{uri}'", ex);
            }
        }

        private async void VerifyCommandExecuted()
        {
            ExceptionlessClient.Default
                .CreateFeatureUsage("TradeList")
                .SetType("Verify")
                .SetProperty("Item", Trade.DumpToText())
                .Submit();

            VerificationState = PoeItemVerificationState.InProgress;
            var verificationResult = await itemVerifier.Verify(Trade);

            if (verificationResult == true)
            {
                VerificationState = PoeItemVerificationState.Verified;
            }
            else if (verificationResult == false)
            {
                VerificationState = PoeItemVerificationState.Sold;
            }
            else
            {
                VerificationState = PoeItemVerificationState.Unknown;
            }
        }
    }
}