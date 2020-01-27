using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using JetBrains.Annotations;
using log4net;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using Unity;

namespace PoeShared.Audio.Services
{
    internal sealed class AudioNotificationsManager : DisposableReactiveObject, IAudioNotificationsManager
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AudioNotificationsManager));
        private readonly IAudioPlayer audioPlayer;

        private readonly ConcurrentDictionary<string, byte[]> knownNotifications = new ConcurrentDictionary<string, byte[]>();
        private readonly ISoundLibrarySource soundLibrarySource;

        public AudioNotificationsManager(
            [NotNull] IAudioPlayer audioPlayer,
            [NotNull] ISoundLibrarySource soundLibrarySource,
            [NotNull] IConfigProvider<PoeEyeSharedConfig> poeEyeConfigProvider,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler)
        {
            Guard.ArgumentNotNull(poeEyeConfigProvider, nameof(poeEyeConfigProvider));
            Guard.ArgumentNotNull(bgScheduler, nameof(bgScheduler));
            Guard.ArgumentNotNull(soundLibrarySource, nameof(soundLibrarySource));
            this.audioPlayer = audioPlayer;
            this.soundLibrarySource = soundLibrarySource;

            Log.Info("Initializing sound subsystem...");
            knownNotifications[AudioNotificationType.Silence.ToString()] = new byte[0];

            bgScheduler.Schedule(Initialize).AddTo(Anchors);
        }

        public IEnumerable<string> Notifications => soundLibrarySource.SourceName;

        public void PlayNotification(AudioNotificationType notificationType)
        {
            PlayNotification(notificationType.ToString());
        }

        public void PlayNotification(string notificationName)
        {
            Guard.ArgumentNotNull(notificationName, nameof(notificationName));
            Log.Debug($"Notification of type {notificationName} requested...");

            if (!TryToLoadNotification(notificationName, out var notificationData))
            {
                Log.Warn(
                    $"Unknown notification type - {notificationName}, known notifications: {string.Join(", ", knownNotifications.Keys.Select(x => x.ToString()))}");
                return;
            }

            if (!notificationData.Any())
            {
                Log.Debug($"No sound data loaded for notification of type {notificationData}");
                return;
            }

            Log.Debug($"Starting playback of {notificationName} ({notificationData.Length}b)...");
            audioPlayer.Play(new MemoryStream(notificationData));
        }

        private bool TryToLoadNotification(string notificationName, out byte[] waveData)
        {
            if (knownNotifications.TryGetValue(notificationName, out waveData))
            {
                return true;
            }

            if (soundLibrarySource.TryToLoadSourceByName(notificationName, out waveData))
            {
                knownNotifications[notificationName] = waveData;
                return true;
            }

            waveData = null;
            return false;
        }

        private void Initialize()
        {
            Log.Debug(
                $"Pre-defined notification list: {knownNotifications.Select(x => $"{x.Key} : {x.Value.Length}b")}");

            foreach (var notificationType in soundLibrarySource.SourceName)
            {
                var notificationName = notificationType.ToLower();

                if (!TryToLoadNotification(notificationName, out _))
                {
                    Log.Warn($"Failed to load notification {notificationType}");
                }
            }

            Log.Debug($"Known notification list: {knownNotifications.Select(x => $"{x.Key} : {x.Value.Length}b")}");
            Log.Info($"Loaded {knownNotifications.Count} audio notifications");
        }
    }
}