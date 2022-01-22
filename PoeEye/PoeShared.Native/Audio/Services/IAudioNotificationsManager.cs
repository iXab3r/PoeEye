﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace PoeShared.Audio.Services;

public interface IAudioNotificationsManager
{
    ReadOnlyObservableCollection<string> Notifications { get; }

    Task PlayNotification(AudioNotificationType notificationType);

    Task PlayNotification([NotNull] string notificationName);
        
    /// <param name="volume">Volume, 1.0 is full scale</param>
    Task PlayNotification([NotNull] string notificationName, float volume);
    Task PlayNotification([NotNull] string notificationName, float volume, CancellationToken cancellationToken);
    Task PlayNotification([NotNull] string notificationName, float volume, WaveOutDevice waveOutDevice, CancellationToken cancellationToken);
    string AddFromFile([NotNull] FileInfo soundFile);
}