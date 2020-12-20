using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace PoeShared.Audio.Services
{
    public interface IAudioNotificationsManager
    {
        ReadOnlyObservableCollection<string> Notifications { get; }

        Task PlayNotification(AudioNotificationType notificationType);

        Task PlayNotification([NotNull] string notificationName);

        string AddFromFile([NotNull] FileInfo soundFile);
    }
}