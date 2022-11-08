using NAudio.CoreAudioApi;

namespace PoeShared.Audio.Models;

internal sealed class MMRenderDeviceProvider : MMDeviceProviderBase, IMMRenderDeviceProvider
{
    public MMRenderDeviceProvider() : base(DataFlow.Render)
    {
    }
}