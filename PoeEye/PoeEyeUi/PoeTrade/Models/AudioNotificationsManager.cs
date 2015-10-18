namespace PoeEyeUi.PoeTrade.Models
{
    using System;
    using System.IO;
    using System.Media;
    using System.Windows.Input;

    using PoeShared;

    using Properties;

    using ReactiveUI;

    internal sealed class AudioNotificationsManager : ReactiveObject, IAudioNotificationsManager
    {
        private readonly ReactiveCommand<object> playNotificationCommand;

        private bool isEnabled;

        public AudioNotificationsManager()
        {
            playNotificationCommand = ReactiveCommand.Create();
            playNotificationCommand.Subscribe(_ => PlayNotificationCommandExecuted());
        }

        public ICommand PlayNotificationCommand => playNotificationCommand;

        public bool IsEnabled
        {
            get { return isEnabled; }
            set { this.RaiseAndSetIfChanged(ref isEnabled, value); }
        }

        private void PlayNotificationCommandExecuted()
        {
            Log.Instance.Debug($"[AudioNotificationsManager] Notification requested...");

            if (!isEnabled)
            {
                Log.Instance.Debug($"[AudioNotificationsManager] Playback is disabled ATM, skipping request");
                return;
            }

            Log.Instance.Debug($"[AudioNotificationsManager] Starting playback...");
            using (var stream = new MemoryStream(Resources.whistle))
            using (var notificationSound = new SoundPlayer(stream))
            {
                notificationSound.Play();
            }
        }
    }
}