using System;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Common.Logging;
using Guards;
using JetBrains.Annotations;
using PoeEye.ItemParser.Services;
using PoeShared;
using PoeShared.Audio;
using PoeShared.Common;
using PoeShared.Converters;
using PoeShared.Native;
using PoeShared.PoeTrade;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using PoeShared.UI;
using PoeShared.UI.ViewModels;
using PoeWhisperMonitor.Chat;
using Prism.Commands;
using ReactiveUI;
using Unity.Attributes;

namespace PoeEye.PoeTrade.ViewModels
{
    internal sealed class PoeTradeViewModel : DisposableReactiveObject, IPoeTradeViewModel
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PoeTradeViewModel));

        private static readonly TimeSpan RefreshTimeout = TimeSpan.FromMinutes(1);
        private readonly IPoeChatService chatService;
        private readonly IClipboardManager clipboardManager;
        private readonly IClock clock;
        private readonly IAudioNotificationsManager notificationsManager;

        private PoeTradeState tradeState;

        public PoeTradeViewModel(
            [NotNull] IPoeItem poeItem,
            [NotNull] IPoePriceCalculcator poePriceCalculator,
            [NotNull] IPoeChatService chatService,
            [NotNull] IAudioNotificationsManager notificationsManager,
            [NotNull] IFactory<IImageViewModel, Uri> imageViewModelFactory,
            [NotNull] IFactory<PoeLinksInfoViewModel, IPoeLinksInfo> linksViewModelFactory,
            [NotNull] IFactory<IPoeItemModsViewModel> modsViewModelFactory,
            [NotNull] IPoeItemSerializer itemSerializer,
            [NotNull] IClipboardManager clipboardManager,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler,
            [NotNull] IClock clock)
        {
            Guard.ArgumentNotNull(poeItem, nameof(poeItem));
            Guard.ArgumentNotNull(poePriceCalculator, nameof(poePriceCalculator));
            Guard.ArgumentNotNull(chatService, nameof(chatService));
            Guard.ArgumentNotNull(itemSerializer, nameof(itemSerializer));
            Guard.ArgumentNotNull(notificationsManager, nameof(notificationsManager));
            Guard.ArgumentNotNull(imageViewModelFactory, nameof(imageViewModelFactory));
            Guard.ArgumentNotNull(linksViewModelFactory, nameof(linksViewModelFactory));
            Guard.ArgumentNotNull(modsViewModelFactory, nameof(modsViewModelFactory));
            Guard.ArgumentNotNull(clipboardManager, nameof(clipboardManager));
            Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));
            Guard.ArgumentNotNull(clock, nameof(clock));

            this.chatService = chatService;
            this.notificationsManager = notificationsManager;
            this.clipboardManager = clipboardManager;
            this.clock = clock;
            Trade = poeItem;

            Mods = modsViewModelFactory.Create();
            Mods.Item = Trade;

            CopyPrivateMessageToClipboardCommand = CommandWrapper.Create<bool>(CopyPrivateMessageCommandExecuted);

            OpenForumUriCommand = new DelegateCommand(OpenForumUriCommandExecuted, OpenForumUriCommandCanExecute);

            Uri imageUri;
            if (!string.IsNullOrWhiteSpace(poeItem.ItemIconUri) && Uri.TryCreate(poeItem.ItemIconUri, UriKind.Absolute, out imageUri))
            {
                Image = imageViewModelFactory.Create(imageUri);
                Anchors.Add(Image);
            }

            if (poeItem.Links != null)
            {
                Links = linksViewModelFactory.Create(poeItem.Links);
                Anchors.Add(Links);
            }

            var price = StringToPoePriceConverter.Instance.Convert(poeItem.Price);
            var priceInChaos = poePriceCalculator.GetEquivalentInChaosOrbs(price);
            PriceInChaosOrbs = priceInChaos;

            CopyItemToClipboardCommand = CommandWrapper.Create(ReactiveCommand.CreateFromTask(
                                                                   async () =>
                                                                   {
                                                                       var item = itemSerializer.Serialize(Trade);
                                                                       if (item == null)
                                                                       {
                                                                           throw new FormatException(
                                                                               $"Failed to parse item\n{Trade.DumpToTextRaw()}");
                                                                       }

                                                                       clipboardManager.SetText(item);
                                                                       await Task.Delay(UiConstants.ArtificialVeryShortDelay);
                                                                   }));

            Observable.Timer(DateTimeOffset.Now, RefreshTimeout).ToUnit()
                      .ObserveOn(uiScheduler)
                      .Subscribe(() => this.RaisePropertyChanged(nameof(TimeElapsedSinceLastIndexation)))
                      .AddTo(Anchors);
        }

        public TimeSpan? TimeElapsedSinceFirstIndexation => Trade.FirstSeen == null ? default(TimeSpan?) : clock.Now - Trade.FirstSeen.Value;

        public ICommand OpenForumUriCommand { get; }

        public IPoeItemModsViewModel Mods { get; }

        public ICommand CopyItemToClipboardCommand { get; }

        public ICommand CopyPrivateMessageToClipboardCommand { get; }

        public TimeSpan? TimeElapsedSinceLastIndexation => Trade.Timestamp == null ? TimeSpan.Zero : clock.Now - Trade.Timestamp;

        public PoeTradeState TradeState
        {
            get => tradeState;
            set => this.RaiseAndSetIfChanged(ref tradeState, value);
        }

        public IImageViewModel Image { get; }

        public PoeLinksInfoViewModel Links { get; }

        public PoePrice? PriceInChaosOrbs { get; }

        public IPoeItem Trade { get; }

        private void OpenForumUriCommandExecuted()
        {
            Guard.ArgumentIsTrue(() => OpenForumUriCommandCanExecute());

            Task.Run(() => OpenUri(Trade.TradeForumUri));
        }

        private bool OpenForumUriCommandCanExecute()
        {
            Uri tradeForumUri;
            return Uri.TryCreate(Trade.TradeForumUri, UriKind.Absolute, out tradeForumUri);
        }

        private async Task CopyPrivateMessageCommandExecuted(bool sendMessageToChat)
        {
            var message = PreparePrivateMessage(Trade);
            if (sendMessageToChat)
            {
                Log.Warn($"[PoeTradeViewModel.SendPrivateMessageCommandExecuted] Sending private message '{message}'");
                notificationsManager.PlayNotification(AudioNotificationType.Keyboard);
                var result = await chatService.SendMessage(message);
                if (result != PoeMessageSendStatus.Success)
                {
                    throw new ApplicationException($"Failed to send message, reason: {result}");
                }

                Log.Warn($"[PoeTradeViewModel.SendPrivateMessageCommandExecuted] Sent message, result: {result}");
            }
            else
            {
                Log.Warn($"[PoeTradeViewModel.SendPrivateMessageCommandExecuted] Copying private message '{message}' to clipboard");
                clipboardManager.SetText(message);
                Log.Warn("[PoeTradeViewModel.SendPrivateMessageCommandExecuted] Copied message");
            }
        }

        private void OpenUri(string uri)
        {
            try
            {
                Process.Start(uri);
            }
            catch (Exception ex)
            {
                Log.Warn($"Failed to open forum Uri '{uri}'", ex);
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