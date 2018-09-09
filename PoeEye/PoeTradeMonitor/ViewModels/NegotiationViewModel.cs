using System;
using System.Globalization;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Guards;
using JetBrains.Annotations;
using Unity; using Unity.Resolution; using Unity.Attributes;
using PoeEye.StashGrid.Services;
using PoeEye.TradeMonitor.Models;
using PoeEye.TradeMonitor.Modularity;
using PoeEye.TradeMonitor.Services;
using PoeShared;
using PoeShared.Common;
using PoeShared.Converters;
using PoeShared.Modularity;
using PoeShared.PoeTrade;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.StashApi;
using PoeShared.StashApi.DataTypes;
using PoeShared.UI.ViewModels;
using PoeWhisperMonitor.Chat;
using Prism.Commands;
using ReactiveUI;
using TypeConverter;

namespace PoeEye.TradeMonitor.ViewModels
{
    internal class NegotiationViewModel : DisposableReactiveObject, INegotiationViewModel, IMacroCommandContext
    {
        public static readonly TimeSpan DefaultUpdatePeriod = TimeSpan.FromSeconds(1);
        public static readonly TimeSpan FreshnessPeriod = TimeSpan.FromSeconds(8);

        private readonly IPoeChatService chatService;
        private readonly IClock clock;
        private readonly DelegateCommand highlightCommand;
        private readonly IPoeStashHighlightService highlightService;
        private readonly SerialDisposable highlightServiceAnchor = new SerialDisposable();
        private readonly IFactory<IImageViewModel, Uri> imageFactory;
        private readonly IScheduler uiScheduler;

        private readonly DelegateCommand inviteToPartyCommand;
        private readonly DelegateCommand kickFromPartyCommand;
        private readonly IPoeMacroCommandsProvider macroCommandsProvider;

        private readonly DelegateCommand openChatCommand;
        private readonly IConverter<IStashItem, IPoeItem> poeStashItemToItemConverter;
        private readonly IPoeItemViewModelFactory poeTradeViewModelFactory;
        private readonly IPoePriceCalculcator priceCalculcator;
        private readonly DelegateCommand<MacroMessage?> sendPredefinedMessageCommand;
        private readonly IPoeStashService stashService;
        private readonly DelegateCommand tradeCommand;

        private bool isExpanded;

        private object item;

        private IImageViewModel itemIcon;

        private PoeItemRarity itemRarity;

        private PoeMessageSendStatus messageSendStatus;
        private TradeModel negotiation;

        private PoeItemVerificationState verificationState;

        public NegotiationViewModel(
            TradeModel model,
            [NotNull] IPoeChatService chatService,
            [NotNull] IPoeStashService stashService,
            [NotNull] IPoeMacroCommandsProvider macroCommandsProvider,
            [NotNull] IPoeStashHighlightService highlightService,
            [NotNull] IPoePriceCalculcator priceCalculcator,
            [NotNull] IClock clock,
            [NotNull] IConfigProvider<PoeTradeMonitorConfig> configProvider,
            [NotNull] IPoeItemViewModelFactory poeTradeViewModelFactory,
            [NotNull] IConverter<IStashItem, IPoeItem> poeStashItemToItemConverter,
            [NotNull] IFactory<IImageViewModel, Uri> imageFactory,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(imageFactory, nameof(imageFactory));
            Guard.ArgumentNotNull(macroCommandsProvider, nameof(macroCommandsProvider));
            Guard.ArgumentNotNull(priceCalculcator, nameof(priceCalculcator));
            Guard.ArgumentNotNull(clock, nameof(clock));
            Guard.ArgumentNotNull(highlightService, nameof(highlightService));
            Guard.ArgumentNotNull(priceCalculcator, nameof(priceCalculcator));
            Guard.ArgumentNotNull(chatService, nameof(chatService));
            Guard.ArgumentNotNull(poeStashItemToItemConverter, nameof(poeStashItemToItemConverter));
            Guard.ArgumentNotNull(poeTradeViewModelFactory, nameof(poeTradeViewModelFactory));
            Guard.ArgumentNotNull(stashService, nameof(stashService));
            Guard.ArgumentNotNull(configProvider, nameof(configProvider));
            Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));

            this.chatService = chatService;
            this.stashService = stashService;
            this.macroCommandsProvider = macroCommandsProvider;
            this.highlightService = highlightService;
            this.priceCalculcator = priceCalculcator;
            this.clock = clock;
            this.poeTradeViewModelFactory = poeTradeViewModelFactory;
            this.poeStashItemToItemConverter = poeStashItemToItemConverter;
            this.imageFactory = imageFactory;
            this.uiScheduler = uiScheduler;

            highlightServiceAnchor.AddTo(Anchors);

            inviteToPartyCommand = new DelegateCommand(InviteToPartyCommandExecuted, GetChatServiceAvailability);
            kickFromPartyCommand = new DelegateCommand(KickFromPartyCommandExecuted, GetChatServiceAvailability);
            openChatCommand = new DelegateCommand(OpenChatCommandExecuted, GetChatServiceAvailability);
            tradeCommand = new DelegateCommand(TradeCommandExecuted, GetChatServiceAvailability);
            highlightCommand = new DelegateCommand(HighlightCommandExecuted, HighlightCommandCanExecute);
            sendPredefinedMessageCommand = new DelegateCommand<MacroMessage?>(
                SendPredefinedMessageCommandExecuted, _ => GetChatServiceAvailability());

            chatService
                .WhenAnyValue(x => x.IsAvailable, x => x.IsBusy)
                .Subscribe(
                    () =>
                    {
                        inviteToPartyCommand.RaiseCanExecuteChanged();
                        kickFromPartyCommand.RaiseCanExecuteChanged();
                        openChatCommand.RaiseCanExecuteChanged();
                        tradeCommand.RaiseCanExecuteChanged();
                        sendPredefinedMessageCommand.RaiseCanExecuteChanged();
                    })
                .AddTo(Anchors);

            configProvider
                .WhenChanged
                .ObserveOn(uiScheduler)
                .Subscribe(ApplyConfig)
                .AddTo(Anchors);

            Observable
                .Timer(DefaultUpdatePeriod, DefaultUpdatePeriod)
                .ObserveOn(uiScheduler)
                .Subscribe(() => this.RaisePropertyChanged(nameof(TimeElapsed)))
                .AddTo(Anchors);

            var price = StringToPoePriceConverter.Instance.Convert(model.PositionName);
            ItemName = price.IsEmpty ? (object) model.PositionName : price;

            this.BindPropertyTo(x => x.Price, this, x => x.Negotiation).AddTo(Anchors);
            this.BindPropertyTo(x => x.Offer, this, x => x.Negotiation).AddTo(Anchors);
            this.BindPropertyTo(x => x.NegotiationType, this, x => x.Negotiation).AddTo(Anchors);
            this.BindPropertyTo(x => x.TimeElapsed, this, x => x.Negotiation).AddTo(Anchors);
            this.BindPropertyTo(x => x.TabName, this, x => x.Negotiation).AddTo(Anchors);
            this.BindPropertyTo(x => x.ItemPosition, this, x => x.Negotiation).AddTo(Anchors);

            this.BindPropertyTo(x => x.PriceInChaos, this, x => x.Price).AddTo(Anchors);
            this.BindPropertyTo(x => x.IsFresh, this, x => x.TimeElapsed).AddTo(Anchors);

            this.BindPropertyTo(x => x.CanHighlight, this, x => x.NegotiationType).AddTo(Anchors);
            this.BindPropertyTo(x => x.CanHighlight, this, x => x.ItemPosition).AddTo(Anchors);
            this.BindPropertyTo(x => x.IsHighlighted, this, x => x.TimeElapsed).AddTo(Anchors);

            VerificationState = PoeItemVerificationState.Unknown;
            UpdateModel(model);

            if (NegotiationType == TradeType.Sell)
            {
                stashService
                    .Updates
                    .ObserveOn(uiScheduler)
                    .Subscribe(HandleStashUpdate)
                    .AddTo(Anchors);

                stashService
                    .WhenAnyValue(x => x.IsBusy)
                    .Where(x => x)
                    .Subscribe(_ => VerificationState = PoeItemVerificationState.InProgress)
                    .AddTo(Anchors);
            }
        }

        public ICommand InviteToPartyCommand => inviteToPartyCommand;

        public ICommand KickFromPartyCommand => kickFromPartyCommand;

        public ICommand OpenChatCommand => openChatCommand;

        public ICommand TradeCommand => tradeCommand;

        public ICommand SendPredefinedMessageCommand => sendPredefinedMessageCommand;

        public ICommand HighlightCommand => highlightCommand;

        public TradeType NegotiationType => Negotiation.TradeType;

        public string Offer => Negotiation.Offer;

        public IReactiveList<MacroCommand> MacroCommands => macroCommandsProvider.MacroCommands;

        public TimeSpan TimeElapsed => clock.Now - Negotiation.Timestamp;

        public bool IsFresh => TimeElapsed < FreshnessPeriod;

        public bool CanHighlight => NegotiationType == TradeType.Sell && !ItemPosition.IsEmpty;

        public PoeItemVerificationState VerificationState
        {
            get => verificationState;
            set => this.RaiseAndSetIfChanged(ref verificationState, value);
        }

        public IImageViewModel ItemIcon
        {
            get => itemIcon;
            set => this.RaiseAndSetIfChanged(ref itemIcon, value);
        }

        public object Item
        {
            get => item;
            set => this.RaiseAndSetIfChanged(ref item, value);
        }

        public object ItemName { get; }

        public PoeItemRarity ItemRarity
        {
            get => itemRarity;
            set => this.RaiseAndSetIfChanged(ref itemRarity, value);
        }

        public ItemPosition ItemPosition => new ItemPosition(Negotiation.ItemPosition.X, Negotiation.ItemPosition.Y, 0, 0);

        public string TabName => Negotiation.TabName;

        public IReactiveList<MacroMessage> PredefinedMessages { get; } = new ReactiveList<MacroMessage>();

        public PoeMessageSendStatus MessageSendStatus
        {
            get => messageSendStatus;
            set => this.RaiseAndSetIfChanged(ref messageSendStatus, value);
        }

        public string CharacterName => Negotiation.CharacterName;

        public PoePrice Price => Negotiation.Price;

        public PoePrice PriceInChaos => priceCalculcator.GetEquivalentInChaosOrbs(Price);

        public INegotiationCloseController CloseController { get; private set; }

        public bool IsHighlighted => highlightServiceAnchor.Disposable != null;

        public TradeModel Negotiation
        {
            get => negotiation;
            set => this.RaiseAndSetIfChanged(ref negotiation, value);
        }

        public void UpdateModel(TradeModel model)
        {
            Log.Instance.Debug($"[NegotiationViewModel] Updating underlying model:\nCurrent: {Negotiation.DumpToText()}\nUpdate: {model.DumpToText()}");

            Negotiation = model;
        }

        private void HandleStashUpdate()
        {
            var item = stashService.TryToFindItem(negotiation.TabName, negotiation.ItemPosition.X, negotiation.ItemPosition.Y);

            var itemIconUri = item?.Icon.ToUriOrDefault();
            ItemIcon = itemIconUri == null
                ? null
                : imageFactory.Create(itemIconUri);

            VerificationState = item != null
                ? PoeItemVerificationState.Verified
                : stashService.IsBusy ? PoeItemVerificationState.InProgress : PoeItemVerificationState.Sold;

            ItemRarity = item != null 
                ? item.Rarity 
                : PoeItemRarity.Unknown;

            Item = item != null
                ? BuildItemViewModel(item)
                : null;
        }

        public bool IsExpanded
        {
            get => isExpanded;
            set => this.RaiseAndSetIfChanged(ref isExpanded, value);
        }

        public void SetCloseController(INegotiationCloseController closeController)
        {
            Guard.ArgumentNotNull(closeController, nameof(closeController));

            CloseController = closeController;
        }

        private bool HighlightCommandCanExecute()
        {
            return CanHighlight;
        }

        private void HighlightCommandExecuted()
        {
            if (!HighlightCommandCanExecute())
            {
                return;
            }

            var item = stashService.TryToFindItem(negotiation.TabName, negotiation.ItemPosition.X, negotiation.ItemPosition.Y);
            var tab = stashService.TryToFindTab(negotiation.TabName);

            var position = Negotiation.ItemPosition;
            if (item != null)
            {
                var stashPosition = new ItemPosition(item.X, item.Y, item.Width, item.Height);
                if (!stashPosition.IsEmpty)
                {
                    position = stashPosition;
                }
            }

            if (position.Width == 0 || position.Height == 0)
            {
                position = new ItemPosition(position.X, position.Y, 1, 1);
            }

            var anchors = new CompositeDisposable();
            highlightServiceAnchor.Disposable = anchors;

            var controller = highlightService.AddHighlight(position, tab?.StashType ?? StashTabType.NormalStash);
            controller.IsFresh = true;
            controller.ToolTipText = tab?.Name ?? Negotiation.TabName;

            controller.AddTo(anchors);

            Observable
                .Timer(FreshnessPeriod)
                .ObserveOn(uiScheduler)
                .Subscribe(() => controller.IsFresh = false)
                .AddTo(anchors);

            Observable
                .Timer(TimeSpan.FromMilliseconds(FreshnessPeriod.TotalMilliseconds * 1.25))
                .ObserveOn(uiScheduler)
                .Subscribe(() => highlightServiceAnchor.Disposable = null)
                .AddTo(anchors);
        }

        private object BuildItemViewModel(IStashItem stashItem)
        {
            if (stashItem == null)
            {
                return null;
            }
            var poeItem = poeStashItemToItemConverter.Convert(stashItem);
            if (poeItem is PoeItem)
            {
                (poeItem as PoeItem).Timestamp = stashService.LastUpdateTimestamp;
            }
            return poeTradeViewModelFactory.Create(poeItem);
        }

        private bool GetChatServiceAvailability()
        {
            return chatService.IsAvailable && !chatService.IsBusy;
        }

        private void TradeCommandExecuted()
        {
            SendChatMessage($"/tradewith {Negotiation.CharacterName}");
        }

        private void OpenChatCommandExecuted()
        {
            SendChatMessage($"@{Negotiation.CharacterName} ", false);
        }

        private void InviteToPartyCommandExecuted()
        {
            SendChatMessage($"/invite {Negotiation.CharacterName}");
        }

        private void KickFromPartyCommandExecuted()
        {
            SendChatMessage($"/kick {Negotiation.CharacterName}");
        }

        private void SendPredefinedMessageCommandExecuted(MacroMessage? message)
        {
            if (message == null)
            {
                return;
            }
            SendPredefinedMessage(message.Value);
        }

        private void SendPredefinedMessage(MacroMessage message)
        {
            var messageToSend = message.Text;
            MacroCommands.ForEach(x => messageToSend = x.CleanupText(messageToSend));

            SendChatMessage($"@{Negotiation.CharacterName} {messageToSend}");

            MacroCommands
                .Where(x => x.TryToMatch(message.Text).Success)
                .ForEach(x => x.Execute(this));
        }

        private async void SendChatMessage(string messageToSend, bool terminateWithEnter = true)
        {
            MessageSendStatus = PoeMessageSendStatus.Unknown;
            var result = await chatService.SendMessage(messageToSend, terminateWithEnter);
            HandleSendStatus(messageToSend, result);
        }

        private void HandleSendStatus(string message, PoeMessageSendStatus status)
        {
            Log.Instance.Debug($"[NegotiationViewModel] Message status: {status}, message: '{message}'");
            MessageSendStatus = status;
        }

        private void ApplyConfig(PoeTradeMonitorConfig config)
        {
            PredefinedMessages.Clear();
            config.PredefinedMessages.ForEach(PredefinedMessages.Add);
        }
    }
}