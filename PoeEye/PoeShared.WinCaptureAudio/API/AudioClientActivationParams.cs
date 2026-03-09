using System.Runtime.InteropServices;

namespace PoeShared.WinCaptureAudio.API;

[StructLayout(LayoutKind.Sequential)]
internal record struct AudioClientActivationParams
{
    public AudioClientActivationType ActivationType;
    public AudioClientProcessLoopbackParams ProcessLoopbackParams;
}