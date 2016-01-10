namespace PoeEyeUi.PoeTrade.Models
{
    using System;
    using System.Reactive.Linq;

    using Config;

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
            [NotNull] IPoeEyeConfigProvider poeEyeConfigProvider,
            [NotNull] [Dependency(WellKnownWindows.PathOfExile)] IWindowTracker poeWindowTracker,
            [NotNull] IAudioNotificationsManager audioNotificationsManager)
        {
            Guard.ArgumentNotNull(() => whispers);
            Guard.ArgumentNotNull(() => poeEyeConfigProvider);
            Guard.ArgumentNotNull(() => audioNotificationsManager);
            Guard.ArgumentNotNull(() => poeWindowTracker);

            this.audioNotificationsManager = audioNotificationsManager;

            poeEyeConfigProvider
                .WhenAnyValue(x => x.ActualConfig)
                .Select(x => x.WhisperNotificationsEnabled)
                .Subscribe(newValue => isEnabled = newValue)
                .AddTo(Anchors);

            whispers.Messages
                    .Where(x => isEnabled)
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