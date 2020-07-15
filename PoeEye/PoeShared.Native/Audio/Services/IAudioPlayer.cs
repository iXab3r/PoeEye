using System;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace PoeShared.Audio.Services
{
    internal interface IAudioPlayer : IDisposableReactiveObject
    {
        [NotNull]
        IDisposable Play([NotNull] byte[] waveData);
    }
}