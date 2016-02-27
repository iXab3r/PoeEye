namespace PoeEyeUi.PoeTrade.Models
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Media;
    using System.Reactive.Linq;
    using System.Windows.Input;

    using Config;

    using Guards;

    using JetBrains.Annotations;

    using PoeShared;
    using PoeShared.Scaffolding;

    using Properties;

    using ReactiveUI;

    internal sealed class AudioNotificationsManager : DisposableReactiveObject, IAudioNotificationsManager
    {
        private readonly IDictionary<AudioNotificationType, byte[]> knownNotifications = new Dictionary<AudioNotificationType, byte[]>
        {
            {AudioNotificationType.NewItem, Resources.whistle},
            {AudioNotificationType.Captcha, Resources.sounds_940_pizzicato},
            {AudioNotificationType.Whisper, Resources.icq}
        };

        private readonly ReactiveCommand<AudioNotificationType> playNotificationCommand;

        private bool isEnabled;

        public AudioNotificationsManager([NotNull] IPoeEyeConfigProvider poeEyeConfigProvider)
        {
            Guard.ArgumentNotNull(() => poeEyeConfigProvider);

            var playNotificationCommandCanExecute = this.WhenAnyValue(x => x.IsEnabled);

            playNotificationCommand = new ReactiveCommand<AudioNotificationType>(playNotificationCommandCanExecute, x => Observable.Return((AudioNotificationType) x));
            playNotificationCommand.Subscribe(PlayNotification).AddTo(Anchors);

            poeEyeConfigProvider
                .WhenAnyValue(x => x.ActualConfig)
                .Select(x => x.AudioNotificationsEnabled)
                .DistinctUntilChanged()
                .Subscribe(newValue => IsEnabled = newValue)
                .AddTo(Anchors);
        }

        public ICommand PlayNotificationCommand => playNotificationCommand;

        private bool IsEnabled
        {
            get { return isEnabled; }
            set { this.RaiseAndSetIfChanged(ref isEnabled, value); }
        }

        public void PlayNotification(AudioNotificationType notificationType)
        {
            Log.Instance.Debug($"[AudioNotificationsManager] Notification of type {notificationType} requested...");

            if (!isEnabled)
            {
                Log.Instance.Debug($"[AudioNotificationsManager] Playback is disabled ATM, skipping request");
                return;
            }

            byte[] notificationData;
            if (!knownNotifications.TryGetValue(notificationType, out notificationData))
            {
                Log.Instance.Warn($"[AudioNotificationsManager] Unknown notification type - {notificationType}, known notifications: {string.Join(", ", knownNotifications.Keys)}");
                return;
            }

            Log.Instance.Debug($"[AudioNotificationsManager] Starting playback of {notificationType} ({notificationData.Length}b)...");
            using (var stream = new MemoryStream(notificationData))
            {
                using (var notificationSound = new SoundPlayer(stream))
                {
                    notificationSound.Play();
                }
            }
        }
    }
}