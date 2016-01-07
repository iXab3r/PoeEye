namespace PoeEyeUi.PoeTrade.Models
{
    using System;
    using System.Reactive.Linq;

    using Guards;

    using JetBrains.Annotations;

    using Microsoft.Practices.Unity;

    using PoeShared.Chat;
    using PoeShared.Utilities;

    using PoeWhisperMonitor;

    using Prism;

    using ReactiveUI;

    internal sealed class WhispersNotificationManager : DisposableReactiveObject, IWhispersNotificationManager
    {
        private readonly IAudioNotificationsManager audioNotificationsManager;

        private bool isEnabled;

        public WhispersNotificationManager(
            [NotNull] IPoeWhispers whispers,
            [NotNull] [Dependency(WellKnownWindows.PathOfExile)] IWindowTracker poeWindowTracker,
            [NotNull] IAudioNotificationsManager audioNotificationsManager)
        {
            Guard.ArgumentNotNull(() => whispers);
            Guard.ArgumentNotNull(() => audioNotificationsManager);
            Guard.ArgumentNotNull(() => poeWindowTracker);

            this.audioNotificationsManager = audioNotificationsManager;

            whispers.Messages
                    .Where(x => x.MessageType == PoeMessageType.Whisper)
                    .Where(x => !poeWindowTracker.IsActive)
                    .Subscribe(ProcessWhisper)
                    .AddTo(Anchors);
        }

        public bool IsEnabled
        {
            get { return isEnabled; }
            set { this.RaiseAndSetIfChanged(ref isEnabled, value); }
        }

        private void ProcessWhisper(PoeMessage message)
        {
            audioNotificationsManager.PlayNotification(AudioNotificationType.Whisper);
        }
    }
}