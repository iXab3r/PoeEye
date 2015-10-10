namespace PoeEyeUi.PoeTrade.Models
{
    using System.Media;

    using ReactiveUI;
    using System;
    using System.IO;
    using System.Windows.Input;

    using PoeShared;

    internal sealed class AudioNotificationsManager : ReactiveObject, IAudioNotificationsManager
    {
        private readonly ReactiveCommand<object> playNotificationCommand; 

        public AudioNotificationsManager()
        {
            playNotificationCommand = ReactiveCommand.Create();
            playNotificationCommand.Subscribe(_ => PlayNotificationCommandExecuted());
        }

        public ICommand PlayNotificationCommand => playNotificationCommand;

        private bool isEnabled;

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
            using (var stream = new MemoryStream(Properties.Resources.whistle))
            {
                var notificationSound = new SoundPlayer(stream);
                notificationSound.Play();
            }
        }
    }
}