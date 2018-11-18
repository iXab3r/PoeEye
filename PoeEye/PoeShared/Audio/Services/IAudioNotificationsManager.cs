using System.Collections.Generic;
using JetBrains.Annotations;

namespace PoeShared.Audio.Services
{
    public interface IAudioNotificationsManager
    {
        IEnumerable<string> Notifications { get; }
        
        void PlayNotification(AudioNotificationType notificationType);
        
        void PlayNotification([NotNull] string notificationName);
    }
}