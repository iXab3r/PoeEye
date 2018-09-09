using PoeEye.PoeTrade.Common;
using PoeShared;
using PoeShared.Audio;
using PoeShared.Modularity;
using PoeShared.Prism;
using Unity.Attributes;
using PoeEyeMainConfig = PoeEye.Config.PoeEyeMainConfig;

namespace PoeEye.PoeTrade.Models
{
    using System;
    using System.Reactive.Linq;

    using Config;

    using Guards;

    using JetBrains.Annotations;

    using Unity; using Unity.Resolution; using Unity.Attributes;

    using PoeEye.Prism;

    using PoeShared.Scaffolding;

    using PoeWhisperMonitor;
    using PoeWhisperMonitor.Chat;
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
