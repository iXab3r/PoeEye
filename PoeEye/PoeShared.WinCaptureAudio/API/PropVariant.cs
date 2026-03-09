using System.Runtime.InteropServices;

namespace PoeShared.WinCaptureAudio.API;

[StructLayout(LayoutKind.Explicit)]
internal record struct PropVariant
{
    [FieldOffset(0)] public TagInnerPropVariant inner;
    [FieldOffset(0)] public decimal Value;
}