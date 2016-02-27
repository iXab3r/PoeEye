namespace PoeEyeUi.PoeTrade.Models
{
    using System;
    using System.Reactive.Linq;

    using Config;

    using Guards;

    using JetBrains.Annotations;

    using Microsoft.Practices.Unity;

    using PoeShared.Scaffolding;

    using PoeWhisperMonitor;
    using PoeWhisperMonitor.Chat;

    using Prism;

    internal sealed class WhispersNotificationManager : DisposableReactiveObject, IWhispersNotificationManager
    {
        private readonly IAudioNotificationsManager audioNotificationsManager;

        public WhispersNotificationManager(
            [NotNull] IPoeWhispers whispers,
            [NotNull] IPoeEyeConfigProvider poeEyeConfigProvider,
            [NotNull] [Dependency(WellKnownWindows.PathOfExile)] IWindowTracker poeWindowTracker,
            [NotNull] IAudioNotificationsManager audioNotificationsManager)
        {
            Guard.ArgumentNotNull(() => whispers);
            Guard.ArgumentNotNull(() => poeEyeConfigProvider);
            Guard.ArgumentNotNull(() => audioNotificationsManager);
            Guard.ArgumentNotNull(() => poeWindowTracker);

            this.audioNotificationsManager = audioNotificationsManager;

            whispers.Messages
                    .Where(x => poeEyeConfigProvider.ActualConfig.WhisperNotificationsEnabled)
                    .Where(x => !poeWindowTracker.IsActive)
                    .Where(x => x.MessageType == PoeMessageType.Whisper)
                    .Subscribe(ProcessWhisper)
                    .AddTo(Anchors);
        }

        private void ProcessWhisper(PoeMessage message)
        {
            audioNotificationsManager.PlayNotification(AudioNotificationType.Whisper);
        }
    }
}