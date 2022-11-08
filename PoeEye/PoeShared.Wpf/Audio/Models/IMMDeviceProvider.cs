using JetBrains.Annotations;
using NAudio.CoreAudioApi;
using PoeShared.Scaffolding;

namespace PoeShared.Audio.Models;

public interface IMMCaptureDeviceProvider : IMMDeviceProvider
{
}

public interface IMMRenderDeviceProvider : IMMDeviceProvider
{
}

public interface IMMDeviceProvider
{
    [CanBeNull]
    MMDevice GetMixerControl([NotNull] string lineId);

    [NotNull]
    IReadOnlyObservableCollection<MMDeviceId> Devices { get; }
}