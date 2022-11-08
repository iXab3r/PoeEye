using System.Collections.ObjectModel;
using JetBrains.Annotations;
using NAudio.CoreAudioApi;

namespace PoeShared.Audio.Models;

public interface IMMDeviceProvider
{
    [CanBeNull]
    MMDevice GetMixerControl([NotNull] string lineId);

    [NotNull]
    ReadOnlyObservableCollection<MMDeviceLineData> Microphones { get; }
}