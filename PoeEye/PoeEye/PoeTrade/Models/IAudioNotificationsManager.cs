using PoeEye.PoeTrade.Common;

namespace PoeEye.PoeTrade.Models
{
    internal interface IAudioNotificationsManager
    {
        void PlayNotification(AudioNotificationType notificationType);
    }
}