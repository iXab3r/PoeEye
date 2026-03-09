using System.Runtime.InteropServices;
using System.Security;
using NAudio.CoreAudioApi;

namespace PoeShared.WinCaptureAudio.API;

[ComImport]
[SuppressUnmanagedCodeSecurity]
[Guid("C8ADBD64-E71E-48a0-A4DE-185C395CD317")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAudioCaptureClient
{
    [PreserveSig]
    int GetBuffer(
        out nint data,
        out uint numFramesToRead,
        out AudioClientBufferFlags flags,
        out ulong devicePosition,
        out ulong qpcPosition);

    [PreserveSig]
    int ReleaseBuffer(uint numFramesRead);

    [PreserveSig]
    int GetNextPacketSize(out uint numFramesInNextPacket);
}