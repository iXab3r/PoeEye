using System;
using System.Reactive.Linq;
using Guards;
using JetBrains.Annotations;
using PoeEye.Config;
using PoeShared.Audio;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeWhisperMonitor;
using PoeWhisperMonitor.Chat;
using Unity.Attributes;

namespace PoeEye.PoeTrade.Models
{
    using IPoeEyeMainConfigProvider = IConfigProvider<PoeEyeMainConfig>;

    internal sealed class WhispersNotificationManager : DisposableReactiveObject, IWhispersNotificationManager
    {
        private readonly IAudioNotificationsManager audioNotificationsManager;

        public WhispersNotificationManager(
            [NotNull] IPoeWhisperService whisperService,
            [NotNull] IPoeEyeMainConfigProvider poeEyeConfigProvider,
            [NotNull] [Dependency(WellKnownWindows.PathOfExileWindow)] IWindowTracker poeWindowTracker,
            [NotNull] IAudioNotificationsManager audioNotificationsManager)
        {
            Guard.ArgumentNotNull(whisperService, nameof(whisperService));
            Guard.ArgumentNotNull(poeEyeConfigProvider, nameof(poeEyeConfigProvider));
            Guard.ArgumentNotNull(audioNotificationsManager, nameof(audioNotificationsManager));
            Guard.ArgumentNotNull(poeWindowTracker, nameof(poeWindowTracker));

            this.audioNotificationsManager = audioNotificationsManager;

            whisperService.Messages
                    .Where(x => poeEyeConfigProvider.ActualConfig.WhisperNotificationsEnabled)
                    .Where(x => !poeWindowTracker.IsActive)
                    .Where(x => x.MessageType == PoeMessageType.WhisperIncoming)
                    .Subscribe(ProcessWhisper)
                    .AddTo(Anchors);
        }

        private void ProcessWhisper(PoeMessage message)
        {
            audioNotificationsManager.PlayNotification(AudioNotificationType.Whisper);
        }
    }
}
