using System;
using System.Reactive.Linq;
using Common.Logging;
using Guards;
using JetBrains.Annotations;
using Microsoft.SqlServer.Server;
using PoeEye.Config;
using PoeShared;
using PoeShared.Audio.Services;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeWhisperMonitor;
using PoeWhisperMonitor.Chat;
using Unity.Attributes;

namespace PoeEye.PoeTrade.Models
{
    internal sealed class AutoInviteService : DisposableReactiveObject, IAutoInviteService
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AutoInviteService));

        private readonly IAudioNotificationsManager audioNotificationsManager;

        public AutoInviteService(
            [NotNull] IPoeWhisperService whisperService,
            [NotNull] IConfigProvider<PoeEyeMainConfig> poeEyeConfigProvider,
            [NotNull] IPoeChatService chatService,
            [NotNull] [Dependency(WellKnownWindows.PathOfExileWindow)] IWindowTracker poeWindowTracker,
            [NotNull] IAudioNotificationsManager audioNotificationsManager)
        {
            Guard.ArgumentNotNull(whisperService, nameof(whisperService));
            Guard.ArgumentNotNull(poeEyeConfigProvider, nameof(poeEyeConfigProvider));
            Guard.ArgumentNotNull(chatService, nameof(chatService));
            Guard.ArgumentNotNull(audioNotificationsManager, nameof(audioNotificationsManager));
            Guard.ArgumentNotNull(poeWindowTracker, nameof(poeWindowTracker));

            this.audioNotificationsManager = audioNotificationsManager;

            whisperService.Messages
                          .Where(x => !string.IsNullOrEmpty(poeEyeConfigProvider.ActualConfig.AutoInviteKeyword))
                          .Where(x => x.MessageType == PoeMessageType.WhisperIncoming)
                          .Where(x => string.Equals(poeEyeConfigProvider.ActualConfig.AutoInviteKeyword, x.Message, StringComparison.InvariantCultureIgnoreCase))
                          .Subscribe(async x =>
                          {
                              Log.Info($"Received auto-invite PM '{x.Message}' ({x.DumpToTextRaw()}), inviting {x.Name}");
                              audioNotificationsManager.PlayNotification(AudioNotificationType.Keyboard);
                              await chatService.SendMessage($"/invite {x.Name}");
                          }, Log.HandleUiException)
                          .AddTo(Anchors);
        }
    }
}