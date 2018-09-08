using System.Threading;
using PoeEye.Converters;
using PoeEye.ItemParser;
using PoeEye.ItemParser.Services;
using PoeEye.PoeTrade.Common;
using PoeShared.Audio;
using PoeShared.Converters;
using PoeShared.PoeTrade;
using PoeShared.Scaffolding.WPF;
using PoeShared.UI;
using PoeShared.UI.ViewModels;
using PoeWhisperMonitor.Chat;
using Prism.Commands;
using ReactiveUI.Legacy;

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
        private readonly IClipboardManager clipboardManager;
        private readonly IClock clock;
        private readonly ReactiveCommand<object> copyPrivateMessageToClipboardCommand = ReactiveUI.Legacy.ReactiveCommand.Create();
        private readonly ReactiveCommand<object> sendPrivateMessageCommand = ReactiveUI.Legacy.ReactiveCommand.Create();

        private readonly ReactiveCommand<object> openForumUriCommand;

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

            copyPrivateMessageToClipboardCommand.Subscribe(CopyPrivateMessageToClipboardCommandExecuted).AddTo(Anchors);
            sendPrivateMessageCommand.Subscribe(SendPrivateMessageCommandExecuted).AddTo(Anchors);

            openForumUriCommand = ReactiveUI.Legacy.ReactiveCommand.Create(Observable.Return(OpenForumUriCommandCanExecute()));
            openForumUriCommand.Subscribe(OpenForumUriCommandExecuted).AddTo(Anchors);

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
                        throw new FormatException($"Failed to parse item\n{Trade.DumpToTextRaw()}");
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

        public TimeSpan TimeElapsedSinceLastIndexation => Trade.Timestamp == DateTime.MinValue ? TimeSpan.Zero : clock.Now - Trade.Timestamp;

        public ICommand OpenForumUriCommand => openForumUriCommand;

        public PoeTradeState TradeState
        {
            get { return tradeState; }
            set { this.RaiseAndSetIfChanged(ref tradeState, value); }
        }

        public IImageViewModel Image { get; }

        public PoeLinksInfoViewModel Links { get; }
        
        public IPoeItemModsViewModel Mods { get; }

        public PoePrice? PriceInChaosOrbs { get; }

        public IPoeItem Trade { get; }

        public ICommand CopyPrivateMessageToClipboardCommand => copyPrivateMessageToClipboardCommand;
        
        public ICommand CopyItemToClipboardCommand { get; }

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

        private async void SendPrivateMessageCommandExecuted(object arg)
        {
            ExceptionlessClient.Default
                .CreateFeatureUsage("TradeList")
                .SetType("SendPrivateMesage")
                .SetProperty("Item", Trade.DumpToText())
                .Submit();

            var message = PreparePrivateMessage(Trade);
            try
            {
                Log.Instance.Warn($"[PoeTradeViewModel.SendPrivateMessageCommandExecuted] Sending private message '{message}'");
                notificationsManager.PlayNotification(AudioNotificationType.Keyboard);
                var result = await chatService.SendMessage(message);
                Log.Instance.Warn($"[PoeTradeViewModel.SendPrivateMessageCommandExecuted] Sent message, result: {result}");
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
                clipboardManager.SetText(message);
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
