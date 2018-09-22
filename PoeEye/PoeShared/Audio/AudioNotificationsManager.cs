using System;
using System.Collections.Concurrent;
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
        private static readonly ILog Log = LogManager.GetLogger(typeof(AudioNotificationsManager));

        private readonly ConcurrentDictionary<AudioNotificationType, byte[]> knownNotifications = new ConcurrentDictionary<AudioNotificationType, byte[]>();

        public AudioNotificationsManager(
            [NotNull] IConfigProvider<PoeEyeSharedConfig> poeEyeConfigProvider,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler)
        {
            Guard.ArgumentNotNull(poeEyeConfigProvider, nameof(poeEyeConfigProvider));
            Guard.ArgumentNotNull(bgScheduler, nameof(bgScheduler));

            Log.Info("Initializing sound subsystem...");
            knownNotifications[AudioNotificationType.Silence] = new byte[0];

            bgScheduler.Schedule(Initialize).AddTo(Anchors);
        }

        public void PlayNotification(AudioNotificationType notificationType)
        {
            Log.Debug($"Notification of type {notificationType} requested...");

            if (!knownNotifications.TryGetValue(notificationType, out var notificationData))
            {
                Log.Warn(
                    $"Unknown notification type - {notificationType}, known notifications: {string.Join(", ", knownNotifications.Keys.Select(x => x.ToString()))}");
                return;
            }

            if (!notificationData.Any())
            {
                Log.Debug($"No sound data loaded for notification of type {notificationData}");
                return;
            }

            Log.Debug(
                $"Starting playback of {notificationType} ({notificationData.Length}b)...");
            using (var stream = new MemoryStream(notificationData))
            using (var notificationSound = new SoundPlayer(stream))
            {
                notificationSound.Play();
            }
        }

        private void Initialize()
        {
            Log.Debug(
                $"Pre-defined notification list: {knownNotifications.Select(x => $"{x.Key} : {x.Value.Length}b")}");

            foreach (var notificationType in Enum.GetValues(typeof(AudioNotificationType)).Cast<AudioNotificationType>())
            {
                var notificationName = notificationType.ToString().ToLower();
                if (!SoundLibrary.TryToLoadSoundByName(notificationName, out var soundData))
                {
                    Log.Warn($"Failed to load notification {notificationType}");
                    continue;
                }

                knownNotifications[notificationType] = soundData;
            }

            Log.Debug(
                $"Known notification list: {knownNotifications.Select(x => $"{x.Key} : {x.Value.Length}b")}");
            Log.Info($"Loaded {knownNotifications.Count} audio notifications");
        }
    }
}