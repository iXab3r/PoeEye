using System.Collections.ObjectModel;
using JetBrains.Annotations;
using NAudio.CoreAudioApi;

namespace PoeShared.Audio.Models;

public interface IMicrophoneProvider
{
    [CanBeNull]
    MMDevice GetMixerControl([NotNull] string lineId);

    [NotNull]
    ReadOnlyObservableCollection<MicrophoneLineData> Microphones { get; }
}