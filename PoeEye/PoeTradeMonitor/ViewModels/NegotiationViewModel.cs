﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Input;
using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeEye.TradeMonitor.Models;
using PoeEye.TradeMonitor.Modularity;
using PoeShared;
using PoeShared.Common;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeWhisperMonitor.Chat;
using Prism.Commands;
using ReactiveUI;

namespace PoeEye.TradeMonitor.ViewModels
{
    internal class NegotiationViewModel : DisposableReactiveObject, INegotiationViewModel, IMacroCommandContext
    {
        public static readonly TimeSpan DefaultUpdatePeriod = TimeSpan.FromSeconds(1);

        private readonly TradeModel model;
        private readonly IPoeChatService chatService;
        private readonly IClock clock;

        private readonly DelegateCommand inviteToPartyCommand;
        private readonly DelegateCommand kickFromPartyCommand;
        private readonly DelegateCommand openChatCommand;
        private readonly DelegateCommand tradeCommand;
        private readonly DelegateCommand<MacroMessage?> sendPredefinedMessageCommand;
        private bool isExpanded;
        private INegotiationCloseController closeController;

        public NegotiationViewModel(
            TradeModel model,
            [NotNull] IPoeChatService chatService,
            [NotNull] IClock clock,
            [NotNull] IConfigProvider<PoeTradeMonitorConfig> configProvider,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(() => clock);
            Guard.ArgumentNotNull(() => chatService);
            Guard.ArgumentNotNull(() => configProvider);

            this.model = model;
            this.chatService = chatService;
            this.clock = clock;

            inviteToPartyCommand = new DelegateCommand(InviteToPartyCommandExecuted, GetChatServiceAvailability);
            kickFromPartyCommand = new DelegateCommand(KickFromPartyCommandExecuted, GetChatServiceAvailability);
            openChatCommand = new DelegateCommand(OpenChatCommandExecuted, GetChatServiceAvailability);
            tradeCommand = new DelegateCommand(TradeCommandExecuted, GetChatServiceAvailability);
            sendPredefinedMessageCommand = new DelegateCommand<MacroMessage?>(SendPredefinedMessageCommandExecuted, _ => GetChatServiceAvailability());

            chatService
                .WhenAnyValue(x => x.IsAvailable)
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
                .WhenAnyValue(x => x.ActualConfig)
                .Subscribe(ApplyConfig)
                .AddTo(Anchors);

            Observable
                .Timer(DefaultUpdatePeriod, DefaultUpdatePeriod)
                .ObserveOn(uiScheduler)
                .Subscribe(() => this.RaisePropertyChanged(nameof(TimeElapsed)))
                .AddTo(Anchors);
        }

        public bool IsExpanded
        {
            get { return isExpanded; }
            set { this.RaiseAndSetIfChanged(ref isExpanded, value); }
        }

        public ICommand InviteToPartyCommand => inviteToPartyCommand;

        public ICommand KickFromPartyCommand => kickFromPartyCommand;

        public ICommand OpenChatCommand => openChatCommand;

        public ICommand TradeCommand => tradeCommand;

        public ICommand SendPredefinedMessageCommand => sendPredefinedMessageCommand;

        public string CharacterName => model.CharacterName;

        public TimeSpan TimeElapsed => clock.Now - model.Timestamp;

        public string ItemName => model.ItemName;

        public PoePrice Price => model.Price;

        public IReactiveList<MacroMessage> PredefinedMessages { get; } = new ReactiveList<MacroMessage>();

        public IReactiveList<MacroCommand> Commands { get; } = new ReactiveList<MacroCommand>();

        public void SetCloseController(INegotiationCloseController closeController)
        {
            Guard.ArgumentNotNull(() => closeController);

            this.closeController = closeController;
        }

        private bool GetChatServiceAvailability()
        {
            return chatService.IsAvailable;
        }

        private void TradeCommandExecuted()
        {
            chatService.SendMessage($"/trade {model.CharacterName}");
        }

        private void OpenChatCommandExecuted()
        {
            chatService.SendMessage($"@{model.CharacterName} ", false);
        }

        private void InviteToPartyCommandExecuted()
        {
            chatService.SendMessage($"/invite {model.CharacterName}");
        }

        private void KickFromPartyCommandExecuted()
        {
            chatService.SendMessage($"/kick {model.CharacterName}");
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
            Commands.ForEach(x => messageToSend = x.CleanupText(messageToSend));

            chatService.SendMessage($"@{model.CharacterName} {messageToSend}");

            Commands
                .Where(x => x.TryToMatch(message.Text).Success)
                .ForEach(x => x.Execute(this));
        }

        private void ApplyConfig(PoeTradeMonitorConfig config)
        {
            PredefinedMessages.Clear();
            config.PredefinedMessages.ForEach(PredefinedMessages.Add);
        }

        public INegotiationCloseController CloseController => closeController;

        public TradeModel Negotiation => model;
    }
}