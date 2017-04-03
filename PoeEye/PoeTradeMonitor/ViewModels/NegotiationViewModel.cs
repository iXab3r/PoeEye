﻿using System;
using System.Globalization;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Windows.Input;
using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
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

        private readonly IPoeChatService chatService;
        private readonly IClock clock;
        private readonly IFactory<IImageViewModel, Uri> imageFactory;

        private readonly DelegateCommand inviteToPartyCommand;
        private readonly DelegateCommand kickFromPartyCommand;
        private readonly IPoeMacroCommandsProvider macroCommandsProvider;

        private readonly DelegateCommand openChatCommand;
        private readonly IConverter<IStashItem, IPoeItem> poeStashItemToItemConverter;
        private readonly IPoeItemViewModelFactory poeTradeViewModelFactory;
        private readonly DelegateCommand<MacroMessage?> sendPredefinedMessageCommand;
        private readonly IPoeStashService stashService;
        private readonly DelegateCommand tradeCommand;

        private bool isExpanded;

        private object item;

        private IImageViewModel itemIcon;

        private PoeItemRarity itemRarity;

        private PoeMessageSendStatus messageSendStatus;

        public NegotiationViewModel(
            TradeModel model,
            [NotNull] IPoeChatService chatService,
            [NotNull] IPoeStashService stashService,
            [NotNull] IPoeMacroCommandsProvider macroCommandsProvider,
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
            Guard.ArgumentNotNull(chatService, nameof(chatService));
            Guard.ArgumentNotNull(poeStashItemToItemConverter, nameof(poeStashItemToItemConverter));
            Guard.ArgumentNotNull(poeTradeViewModelFactory, nameof(poeTradeViewModelFactory));
            Guard.ArgumentNotNull(stashService, nameof(stashService));
            Guard.ArgumentNotNull(configProvider, nameof(configProvider));

            Negotiation = model;
            this.chatService = chatService;
            this.stashService = stashService;
            this.macroCommandsProvider = macroCommandsProvider;
            this.clock = clock;
            this.poeTradeViewModelFactory = poeTradeViewModelFactory;
            this.poeStashItemToItemConverter = poeStashItemToItemConverter;
            this.imageFactory = imageFactory;

            PriceInChaos = priceCalculcator.GetEquivalentInChaosOrbs(model.Price);

            inviteToPartyCommand = new DelegateCommand(InviteToPartyCommandExecuted, GetChatServiceAvailability);
            kickFromPartyCommand = new DelegateCommand(KickFromPartyCommandExecuted, GetChatServiceAvailability);
            openChatCommand = new DelegateCommand(OpenChatCommandExecuted, GetChatServiceAvailability);
            tradeCommand = new DelegateCommand(TradeCommandExecuted, GetChatServiceAvailability);
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

            stashService
                .Updates
                .ObserveOn(uiScheduler)
                .Subscribe(HandleStashUpdate)
                .AddTo(Anchors);

            stashService
                .WhenAnyValue(x => x.IsBusy)
                .ObserveOn(uiScheduler)
                .Subscribe(() => this.RaisePropertyChanged(nameof(IsBusy)))
                .AddTo(Anchors);

            var price = PriceToCurrencyConverter.Instance.Convert(model.PositionName);
            ItemName = price.IsEmpty ? (object) model.PositionName : price;
        }

        public ICommand InviteToPartyCommand => inviteToPartyCommand;

        public ICommand KickFromPartyCommand => kickFromPartyCommand;

        public ICommand OpenChatCommand => openChatCommand;

        public ICommand TradeCommand => tradeCommand;

        public ICommand SendPredefinedMessageCommand => sendPredefinedMessageCommand;

        public TradeType NegotiationType => Negotiation.TradeType;

        public string Offer => Negotiation.Offer;

        public IReactiveList<MacroCommand> MacroCommands => macroCommandsProvider.MacroCommands;

        public TimeSpan TimeElapsed => clock.Now - Negotiation.Timestamp;

        public bool IsBusy => stashService.IsBusy;

        public IImageViewModel ItemIcon
        {
            get { return itemIcon; }
            set { this.RaiseAndSetIfChanged(ref itemIcon, value); }
        }

        public object Item
        {
            get { return item; }
            set { this.RaiseAndSetIfChanged(ref item, value); }
        }

        public object ItemName { get; }

        public PoeItemRarity ItemRarity
        {
            get { return itemRarity; }
            set { this.RaiseAndSetIfChanged(ref itemRarity, value); }
        }

        public ItemPosition ItemPosition => Negotiation.ItemPosition;

        public string TabName => Negotiation.TabName;

        public IReactiveList<MacroMessage> PredefinedMessages { get; } = new ReactiveList<MacroMessage>();

        public PoeMessageSendStatus MessageSendStatus
        {
            get { return messageSendStatus; }
            set { this.RaiseAndSetIfChanged(ref messageSendStatus, value); }
        }

        public INegotiationCloseController CloseController { get; private set; }

        public TradeModel Negotiation { get; }

        public bool IsExpanded
        {
            get { return isExpanded; }
            set { this.RaiseAndSetIfChanged(ref isExpanded, value); }
        }

        public string CharacterName => Negotiation.CharacterName;

        public PoePrice Price => Negotiation.Price;

        public PoePrice PriceInChaos { get; }

        public void SetCloseController(INegotiationCloseController closeController)
        {
            Guard.ArgumentNotNull(closeController, nameof(closeController));

            CloseController = closeController;
        }

        private void HandleStashUpdate()
        {
            var item = stashService.TryToFindItem(
                Negotiation.TabName, Negotiation.ItemPosition.X, Negotiation.ItemPosition.Y);

            var itemIconUri = item?.Icon.ToUriOrDefault();
            ItemIcon = itemIconUri == null
                ? null
                : imageFactory.Create(itemIconUri);

            ItemRarity = item?.Rarity ?? PoeItemRarity.Unknown;

            var lastUpdateTimeStampInfo = stashService.LastUpdateTimestamp == DateTime.MinValue
                ? "Never"
                : stashService.LastUpdateTimestamp.ToString(CultureInfo.InvariantCulture);

            Item = item != null
                ? BuildItemViewModel(item)
                : null;
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
