using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace PoeShared.Audio.Services
{
    internal interface IAudioPlayer : IDisposableReactiveObject
    {
        [NotNull]
        Task Play([NotNull] byte[] waveData);
        
        [NotNull]
        Task Play([NotNull] byte[] waveData, float volume);
    }
}