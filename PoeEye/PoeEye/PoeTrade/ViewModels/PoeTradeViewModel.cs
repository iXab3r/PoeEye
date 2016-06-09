using System.Threading;
using PoeEye.Converters;
using PoeWhisperMonitor.Chat;

namespace PoeEye.PoeTrade.ViewModels
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

    using PoeEye.Prism;

    using PoeShared;
    using PoeShared.Common;
    using PoeShared.Prism;
    using PoeShared.Scaffolding;

    using ReactiveUI;

    internal sealed class PoeTradeViewModel : DisposableReactiveObject, IPoeTradeViewModel
    {
        private static readonly TimeSpan RefreshTimeout = TimeSpan.FromMinutes(1);
        private readonly IPoeChatService chatService;
        private readonly IAudioNotificationsManager notificationsManager;
        private readonly IClock clock;
        private readonly ReactiveCommand<object> copyPrivateMessageToClipboardCommand = ReactiveCommand.Create();
        private readonly ReactiveCommand<object> sendPrivateMessageCommand = ReactiveCommand.Create();

        private readonly ReactiveCommand<object> openForumUriCommand;

        private PoeTradeState tradeState;

        public PoeTradeViewModel(
            [NotNull] IPoeItem poeItem,
            [NotNull] IPoePriceCalculcator poePriceCalculcator,
            [NotNull] IPoeChatService chatService,
            [NotNull] IAudioNotificationsManager notificationsManager,
            [NotNull] IFactory<ImageViewModel, Uri> imageViewModelFactory,
            [NotNull] IFactory<PoeLinksInfoViewModel, IPoeLinksInfo> linksViewModelFactory,
            [NotNull] [Dependency(WellKnownSchedulers.Ui)] IScheduler uiScheduler,
            [NotNull] IClock clock)
        {
            Guard.ArgumentNotNull(() => poeItem);
            Guard.ArgumentNotNull(() => poePriceCalculcator);
            Guard.ArgumentNotNull(() => chatService);
            Guard.ArgumentNotNull(() => notificationsManager);
            Guard.ArgumentNotNull(() => imageViewModelFactory);
            Guard.ArgumentNotNull(() => linksViewModelFactory);
            Guard.ArgumentNotNull(() => uiScheduler);
            Guard.ArgumentNotNull(() => clock);

            this.chatService = chatService;
            this.notificationsManager = notificationsManager;
            this.clock = clock;
            Trade = poeItem;

            copyPrivateMessageToClipboardCommand.Subscribe(CopyPrivateMessageToClipboardCommandExecuted).AddTo(Anchors);
            sendPrivateMessageCommand.Subscribe(SendPrivateMessageCommandExecuted).AddTo(Anchors);

            openForumUriCommand = ReactiveCommand.Create(Observable.Return(OpenForumUriCommandCanExecute()));
            openForumUriCommand.Subscribe(OpenForumUriCommandExecuted).AddTo(Anchors);

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

            var price = PriceToCurrencyConverter.Instance.Convert(poeItem.Price);
            var priceInChaos = poePriceCalculcator.GetEquivalentInChaosOrbs(price);
            PriceInChaosOrbs = price.CurrencyType == KnownCurrencyNameList.ChaosOrb 
                ? default(PoePrice?) 
                : priceInChaos;
            RawPriceInChaosOrbs = priceInChaos.Value;

            Observable.Timer(DateTimeOffset.Now, RefreshTimeout).ToUnit()
                      .ObserveOn(uiScheduler)
                      .Subscribe(() => this.RaisePropertyChanged(nameof(TimeElapsedSinceLastIndexation)))
                      .AddTo(Anchors);
        }

        public TimeSpan? TimeElapsedSinceFirstIndexation => Trade.FirstSeen == null ? default(TimeSpan?) : clock.Now - Trade.FirstSeen.Value;

        public TimeSpan TimeElapsedSinceLastIndexation => Trade.Timestamp == DateTime.MinValue ? TimeSpan.Zero : clock.Now - Trade.Timestamp;

        public ICommand OpenForumUriCommand => openForumUriCommand;

        public PoeTradeState TradeState
        {
            get { return tradeState; }
            set { this.RaiseAndSetIfChanged(ref tradeState, value); }
        }

        public ImageViewModel ImageViewModel { get; }

        public PoeLinksInfoViewModel LinksViewModel { get; }

        public PoePrice? PriceInChaosOrbs { get; }

        public float RawPriceInChaosOrbs { get; }

        public IPoeItemMod[] ImplicitMods => Trade.Mods.Where(x => x.ModType == PoeModType.Implicit).ToArray();

        public IPoeItemMod[] ExplicitMods => Trade.Mods.Where(x => x.ModType == PoeModType.Explicit).ToArray();

        public IPoeItem Trade { get; }

        public ICommand CopyPrivateMessageToClipboardCommand => copyPrivateMessageToClipboardCommand;

        public ICommand SendPrivateMessageCommand => sendPrivateMessageCommand;

        private void OpenForumUriCommandExecuted(object arg)
        {
            Guard.ArgumentIsTrue(() => OpenForumUriCommandCanExecute());

            Task.Run(() => OpenUri(Trade.TradeForumUri));
        }

        private bool OpenForumUriCommandCanExecute()
        {
            Uri tradeForumUri;
            return Uri.TryCreate(Trade.TradeForumUri, UriKind.Absolute, out tradeForumUri);
        }

        private void SendPrivateMessageCommandExecuted(object arg)
        {
            ExceptionlessClient.Default
                .CreateFeatureUsage("TradeList")
                .SetType("SendPrivateMesage")
                .SetProperty("Item", Trade.DumpToText())
                .Submit();

            var message = "test" ?? PreparePrivateMessage(Trade);
            try
            {
                notificationsManager.PlayNotification(AudioNotificationType.Keyboard);
                var result = chatService.SendMessage(message);
            }
            catch (Exception ex)
            {
                Log.Instance.Warn($"Failed to send private message '{message}'", ex);
            }
        }

        private void CopyPrivateMessageToClipboardCommandExecuted(object arg)
        {
            ExceptionlessClient.Default
                .CreateFeatureUsage("TradeList")
                .SetType("CopyToClipboard")
                .SetProperty("Item", Trade.DumpToText())
                .Submit();

            var message = PreparePrivateMessage(Trade);
            try
            {
                Clipboard.SetText(message);
            }
            catch (Exception ex)
            {
                Log.Instance.Warn($"Failed to send private message '{message}'", ex);
            }
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

        private static string PreparePrivateMessage(IPoeItem trade)
        {
            string message;
            if (!string.IsNullOrWhiteSpace(trade.SuggestedPrivateMessage))
            {
                message = trade.SuggestedPrivateMessage;
            }
            else
            {
                message = string.IsNullOrWhiteSpace(trade.Price)
                    ? $"@{trade.UserIgn} Hi, I would like to buy your {trade.ItemName} listed in {trade.League}, offer is "
                    : $"@{trade.UserIgn} Hi, I would like to buy your {trade.ItemName} listed for {trade.Price} in {trade.League}";
            }
            return message;
        }
    }
}