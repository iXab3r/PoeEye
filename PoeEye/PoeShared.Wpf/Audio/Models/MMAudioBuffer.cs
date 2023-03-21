using System;

namespace PoeShared.Audio.Models;

public readonly record struct MMAudioBuffer(byte[] Buffer, TimeSpan CaptureTimestamp, TimeSpan? previousTimestamp)
{
    public int BufferLength { get; } = Buffer.Length;

    public TimeSpan TimeSinceLastBuffer { get; } = previousTimestamp == null || previousTimestamp.Value == TimeSpan.Zero ? TimeSpan.Zero : CaptureTimestamp - previousTimestamp.Value;
}