using NAudio.CoreAudioApi;

namespace PoeShared.Audio.Models;

internal sealed class MMCaptureDeviceProvider : MMDeviceProviderBase, IMMCaptureDeviceProvider
{
    public MMCaptureDeviceProvider() : base(DataFlow.Capture)
    {
    }
}