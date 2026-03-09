using System.Runtime.InteropServices;

namespace PoeShared.WinCaptureAudio.API;

[StructLayout(LayoutKind.Sequential)]
internal record struct AudioClientProcessLoopbackParams
{
    public int TargetProcessId;
    public ProcessLoopbackMode ProcessLoopbackMode;
}