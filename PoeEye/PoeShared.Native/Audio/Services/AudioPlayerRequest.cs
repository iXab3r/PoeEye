using System.Threading;

namespace PoeShared.Audio.Services;

public sealed record AudioPlayerRequest
{
    public byte[] WaveData
    {
        get;
        init;
    }

    public WaveOutDevice OutputDevice { get; init; }

    public float? Volume { get; init; } 
}