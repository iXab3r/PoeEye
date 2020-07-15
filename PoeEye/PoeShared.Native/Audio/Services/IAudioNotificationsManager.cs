using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using JetBrains.Annotations;

namespace PoeShared.Audio.Services
{
    public interface IAudioNotificationsManager
    {
        ReadOnlyObservableCollection<string> Notifications { get; }

        void PlayNotification(AudioNotificationType notificationType);

        void PlayNotification([NotNull] string notificationName);

        string AddFromFile([NotNull] FileInfo soundFile);
    }
}