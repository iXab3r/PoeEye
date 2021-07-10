using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using log4net;
using PoeShared.Logging;
using PoeShared.Prism;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using Unity;

namespace PoeShared.Audio.Services
{
    internal sealed class AudioNotificationsManager : DisposableReactiveObject, IAudioNotificationsManager
    {
        private static readonly IFluentLog Log = typeof(AudioNotificationsManager).PrepareLogger();

        private readonly IAudioPlayer audioPlayer;
        private readonly ConcurrentDictionary<string, byte[]> knownNotifications = new(StringComparer.OrdinalIgnoreCase);
        private readonly ISoundLibrarySource soundLibrarySource;
        private readonly IFileSoundLibrarySource fileSoundLibrarySource;

        public AudioNotificationsManager(
            [NotNull] IAudioPlayer audioPlayer,
            [NotNull] ISoundLibrarySource soundLibrarySource,
            [NotNull] IFileSoundLibrarySource fileSoundLibrarySource,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler)
        {
            Guard.ArgumentNotNull(bgScheduler, nameof(bgScheduler));
            Guard.ArgumentNotNull(soundLibrarySource, nameof(soundLibrarySource));
            this.audioPlayer = audioPlayer;
            this.soundLibrarySource = soundLibrarySource;
            this.fileSoundLibrarySource = fileSoundLibrarySource;

            Log.Info($"Initializing sound subsystem...");
            knownNotifications[AudioNotificationType.Silence.ToString()] = new byte[0];

            bgScheduler.Schedule(Initialize).AddTo(Anchors);
        }

        public ReadOnlyObservableCollection<string> Notifications => soundLibrarySource.SourceName;

        public Task PlayNotification(AudioNotificationType notificationType)
        {
            return PlayNotification(notificationType.ToString());
        }

        public Task PlayNotification(string notificationName)
        {
            return PlayNotification(notificationName, 1);
        }

        public Task PlayNotification(string notificationName, float volume)
        {
            return PlayNotification(notificationName, volume, CancellationToken.None);
        }

        public Task PlayNotification(string notificationName, float volume, CancellationToken cancellationToken)
        {
            return PlayNotification(notificationName, volume, WaveOutDevice.DefaultDevice, CancellationToken.None);
        }

        public Task PlayNotification(string notificationName, float volume, WaveOutDevice waveOutDevice, CancellationToken cancellationToken)
        {
            Guard.ArgumentNotNull(notificationName, nameof(notificationName));
            Log.Debug($"Notification of type {notificationName} requested...");

            if (!TryToLoadNotification(notificationName, out var notificationData))
            {
                Log.Warn(
                    $"Unknown notification type - {notificationName}, known notifications: {string.Join(", ", knownNotifications.Keys.Select(x => x.ToString()))}");
                return Task.CompletedTask;
            }

            if (!notificationData.Any())
            {
                Log.Debug($"No sound data loaded for notification of type {notificationData}");
                return Task.CompletedTask;
            }

            Log.Debug($"Starting playback of {notificationName} ({notificationData.Length}b)...");
            return audioPlayer.Play(new AudioPlayerRequest()
            {
                Volume = volume,
                CancellationToken = cancellationToken,
                WaveData = notificationData,
                OutputDevice = waveOutDevice
            });
        }

        public string AddFromFile(FileInfo soundFile)
        {
            return fileSoundLibrarySource.AddFromFile(soundFile);
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