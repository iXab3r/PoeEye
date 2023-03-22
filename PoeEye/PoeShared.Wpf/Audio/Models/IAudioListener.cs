using System;
using PoeShared.Scaffolding;

namespace PoeShared.Audio.Models;

public interface IAudioListener : IDisposableReactiveObject
{
    IObservable<MMAudioBuffer> Buffers { get; }
}