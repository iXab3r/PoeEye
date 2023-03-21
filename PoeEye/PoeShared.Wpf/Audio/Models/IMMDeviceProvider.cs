using DynamicData;
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
    IObservableCache<MMDevice, MMDeviceId> DevicesById { get; }
    
    [CanBeNull]
    MMDevice GetMixerControl([NotNull] string lineId);

    MMDevice GetDevice(MMDeviceId deviceId);

    [NotNull]
    IReadOnlyObservableCollection<MMDeviceId> Devices { get; }
}