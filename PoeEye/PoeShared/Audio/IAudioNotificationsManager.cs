using JetBrains.Annotations;

namespace PoeShared.Audio
{
    public interface IAudioNotificationsManager
    {
        void PlayNotification(AudioNotificationType notificationType);
        
        void PlayNotification([NotNull] string notificationName);
    }
}