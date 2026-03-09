using System.Runtime.InteropServices;

namespace PoeShared.WinCaptureAudio.API;

[StructLayout(LayoutKind.Sequential)]
internal record struct Blob
{
    public ulong cbSize;
    public nint pBlobData;
}