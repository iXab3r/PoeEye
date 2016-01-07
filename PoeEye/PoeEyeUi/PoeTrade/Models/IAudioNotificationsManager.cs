namespace PoeEyeUi.PoeTrade.Models
{
    internal interface IAudioNotificationsManager 
    {
        void PlayNotification(AudioNotificationType notificationType);

        bool IsEnabled { get; set; }
    }
}