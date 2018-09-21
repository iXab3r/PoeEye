using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Reactive.Concurrency;
using Common.Logging;
using Guards;
using JetBrains.Annotations;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Resources.Notifications;
using PoeShared.Scaffolding;
using Unity.Attributes;

namespace PoeShared.Audio
{
    internal sealed class AudioNotificationsManager : DisposableReactiveObject, IAudioNotificationsManager
    {
        private static readonly ILog Log = LogManager.GetLogger<AudioNotificationsManager>();

        private readonly ConcurrentDictionary<AudioNotificationType, byte[]> knownNotifications = new ConcurrentDictionary<AudioNotificationType, byte[]>();

        public AudioNotificationsManager(
            [NotNull] IConfigProvider<PoeEyeSharedConfig> poeEyeConfigProvider,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler)
        {
            Guard.ArgumentNotNull(poeEyeConfigProvider, nameof(poeEyeConfigProvider));
            Guard.ArgumentNotNull(bgScheduler, nameof(bgScheduler));

            Log.Debug($"[AudioNotificationsManager..ctor] Initializing sound subsystem...");
            knownNotifications[AudioNotificationType.Silence] = new byte[0];

            bgScheduler.Schedule(Initialize).AddTo(Anchors);
        }

        public void PlayNotification(AudioNotificationType notificationType)
        {
            Log.Debug($"[AudioNotificationsManager] Notification of type {notificationType} requested...");

            if (!knownNotifications.TryGetValue(notificationType, out var notificationData))
            {
                Log.Warn(
                    $"[AudioNotificationsManager] Unknown notification type - {notificationType}, known notifications: {string.Join(", ", knownNotifications.Keys.Select(x => x.ToString()))}");
                return;
            }

            if (!notificationData.Any())
            {
                Log.Debug($"[AudioNotificationsManager] No sound data loaded for notification of type {notificationData}");
                return;
            }

            Log.Debug(
                $"[AudioNotificationsManager] Starting playback of {notificationType} ({notificationData.Length}b)...");
            using (var stream = new MemoryStream(notificationData))
            using (var notificationSound = new SoundPlayer(stream))
            {
                notificationSound.Play();
            }
        }

        private void Initialize()
        {
            Log.Debug(
                $"[AudioNotificationsManager.Initialize] Pre-defined notification list: {knownNotifications.Select(x => $"{x.Key} : {x.Value.Length}b")}");

            foreach (var notificationType in Enum.GetValues(typeof(AudioNotificationType)).Cast<AudioNotificationType>())
            {
                var notificationName = notificationType.ToString().ToLower();
                if (!SoundLibrary.TryToLoadSoundByName(notificationName, out var soundData))
                {
                    Log.Warn($"[AudioNotificationsManager.Initialize] Failed to load notification {notificationType}");
                    continue;
                }

                knownNotifications[notificationType] = soundData;
            }

            Log.Debug(
                $"[AudioNotificationsManager.Initialize] Known notification list: {knownNotifications.Select(x => $"{x.Key} : {x.Value.Length}b")}");
        }
    }
}