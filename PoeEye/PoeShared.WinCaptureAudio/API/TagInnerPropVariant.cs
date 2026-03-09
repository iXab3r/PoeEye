using System.Runtime.InteropServices;

namespace PoeShared.WinCaptureAudio.API;

[StructLayout(LayoutKind.Explicit)]
internal record struct TagInnerPropVariant
{
    [FieldOffset(0)] public ushort vt;
    [FieldOffset(2)] public ushort wReserved1;
    [FieldOffset(4)] public ushort wReserved2;
    [FieldOffset(6)] public ushort wReserved3;
    [FieldOffset(8)] public Blob blob;
}