using System;
using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Windows.Input;
using Guards;
using JetBrains.Annotations;
using PoeShared.Audio;
using PoeShared.Audio.Services;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeWhisperMonitor;
using PoeWhisperMonitor.Chat;
using Prism.Commands;
using ReactiveUI;
using Unity.Attributes;

namespace PoeEye.PoeTrade.ViewModels
{
    internal sealed class PoeChatViewModel : DisposableReactiveObject, IPoeChatViewModel
    {
        private static readonly TimeSpan SendMessageStatusThrottleTimeSpan = TimeSpan.FromSeconds(5);
        private readonly IPoeChatService chatService;

        private readonly SerialDisposable messageQueueDisposable = new SerialDisposable();

        private readonly IAudioNotificationsManager notificationsManager;
        private readonly DelegateCommand<string> sendMessageCommand;
        private string messageToSend;

        private PoeMessageSendStatus sendStatus;

        private string sendStatusErrorMessage;

        public PoeChatViewModel(
            [NotNull] IPoeWhisperService whisperService,
            [NotNull] IAudioNotificationsManager notificationsManager,
            [NotNull] IPoeChatService chatService,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(chatService, nameof(chatService));
            Guard.ArgumentNotNull(whisperService, nameof(whisperService));
            Guard.ArgumentNotNull(notificationsManager, nameof(notificationsManager));
            Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));

            this.notificationsManager = notificationsManager;
            this.chatService = chatService;
            whisperService.Messages
                          .ObserveOn(uiScheduler)
                          .Where(x => x.MessageType == PoeMessageType.WhisperIncoming || x.MessageType == PoeMessageType.WhisperOutgoing)
                          .Subscribe(Messages.Add)
                          .AddTo(Anchors);

            sendMessageCommand = new DelegateCommand<string>(SendMessageCommandExecuted, SendMessageCommandCanExecute);

            chatService.WhenAnyValue(x => x.IsAvailable).ToUnit().Merge(this.WhenAnyValue(x => x.MessageToSend).ToUnit())
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
            get => messageToSend;
            set => this.RaiseAndSetIfChanged(ref messageToSend, value);
        }

        public PoeMessageSendStatus SendStatus
        {
            get => sendStatus;
            set => this.RaiseAndSetIfChanged(ref sendStatus, value);
        }

        public string SendStatusErrorMessage
        {
            get => sendStatusErrorMessage;
            set => this.RaiseAndSetIfChanged(ref sendStatusErrorMessage, value);
        }

        public ObservableCollection<PoeMessage> Messages { get; } = new ObservableCollection<PoeMessage>();

        public ICommand SendMessageCommand => sendMessageCommand;

        private bool SendMessageCommandCanExecute(string message)
        {
            return chatService.IsAvailable && !string.IsNullOrWhiteSpace(message);
        }

        private void SendMessageCommandExecuted(string message)
        {
            messageQueueDisposable.Disposable = chatService
                                                .SendMessage(message)
                                                .ToObservable()
                                                .Subscribe(HandleMessageSendStatus);
        }

        private void HandleMessageSendStatus(PoeMessageSendStatus status)
        {
            SendStatus = status;
            if (SendStatus == PoeMessageSendStatus.Success)
            {
                notificationsManager.PlayNotification(AudioNotificationType.Keyboard);
                MessageToSend = string.Empty;
            }
        }
    }
}