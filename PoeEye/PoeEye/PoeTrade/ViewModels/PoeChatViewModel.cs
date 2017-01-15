using System;
using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows.Input;
using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeEye.PoeTrade.Models;
using PoeEye.Prism;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeWhisperMonitor;
using PoeWhisperMonitor.Chat;
using Prism.Commands;
using ReactiveUI;

namespace PoeEye.PoeTrade.ViewModels
{
    internal sealed class PoeChatViewModel : DisposableReactiveObject, IPoeChatViewModel
    {
        private static readonly TimeSpan SendMessageStatusThrottleTimeSpan = TimeSpan.FromSeconds(5);

        private readonly IAudioNotificationsManager notificationsManager;
        private readonly IPoeChatService chatService;
        private readonly DelegateCommand<string> sendMessageCommand;
        private string messageToSend;

        private PoeMessageSendStatus sendStatus;

        private string sendStatusErrorMessage;

        public PoeChatViewModel(
            [NotNull] IPoeWhisperService whisperService,
            [NotNull] IAudioNotificationsManager notificationsManager,
            [NotNull] IPoeChatService chatService,
            [NotNull] [Dependency(WellKnownSchedulers.Ui)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(() => chatService);
            Guard.ArgumentNotNull(() => whisperService);
            Guard.ArgumentNotNull(() => notificationsManager);
            Guard.ArgumentNotNull(() => uiScheduler);

            this.notificationsManager = notificationsManager;
            this.chatService = chatService;
            whisperService.Messages     
                .ObserveOn(uiScheduler)
                .Where(x => x.MessageType == PoeMessageType.WhisperFrom || x.MessageType == PoeMessageType.WhisperTo)
                .Subscribe(Messages.Add)
                .AddTo(Anchors);

            sendMessageCommand = new DelegateCommand<string>(SendMessageCommandExecuted, SendMessageCommandCanExecute);

            Observable.Merge(
                    chatService.WhenAnyValue(x => x.IsAvailable).ToUnit(),
                    this.WhenAnyValue(x => x.MessageToSend).ToUnit())
                .ObserveOn(uiScheduler)
                .Subscribe(sendMessageCommand.RaiseCanExecuteChanged)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.SendStatus)
                .Where(x => x != PoeMessageSendStatus.Unknown)
                .Throttle(SendMessageStatusThrottleTimeSpan)
                .Subscribe(x => SendStatus = PoeMessageSendStatus.Unknown)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.SendStatus)
                .Subscribe(x => SendStatusErrorMessage = x == PoeMessageSendStatus.Success || x == PoeMessageSendStatus.Unknown ? string.Empty : x.ToString())
                .AddTo(Anchors);
        }

        public string MessageToSend
        {
            get { return messageToSend; }
            set { this.RaiseAndSetIfChanged(ref messageToSend, value); }
        }

        public PoeMessageSendStatus SendStatus
        {
            get { return sendStatus; }
            set { this.RaiseAndSetIfChanged(ref sendStatus, value); }
        }

        public string SendStatusErrorMessage
        {
            get { return sendStatusErrorMessage; }
            set { this.RaiseAndSetIfChanged(ref sendStatusErrorMessage, value); }
        }

        public ObservableCollection<PoeMessage> Messages { get; } = new ObservableCollection<PoeMessage>();

        public ICommand SendMessageCommand => sendMessageCommand;

        private bool SendMessageCommandCanExecute(string message)
        {
            return chatService.IsAvailable && !string.IsNullOrWhiteSpace(message);
        }

        private void SendMessageCommandExecuted(string message)
        {
            SendStatus = chatService.SendMessage(message);
            if (SendStatus == PoeMessageSendStatus.Success)
            {
                notificationsManager.PlayNotification(AudioNotificationType.Keyboard);
                MessageToSend = string.Empty;
            }
        }
    }
}