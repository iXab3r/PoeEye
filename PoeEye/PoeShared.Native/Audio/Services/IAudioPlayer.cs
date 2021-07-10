using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PoeShared.Scaffolding; 
using PoeShared.Logging;

namespace PoeShared.Audio.Services
{
    public interface IAudioPlayer : IDisposableReactiveObject
    {
        IEnumerable<WaveOutDevice> GetDevices();
        
        [NotNull]
        Task Play([NotNull] byte[] waveData);
        
        [NotNull]
        Task Play([NotNull] byte[] waveData, float volume);
        
        Task Play(AudioPlayerRequest request);
    }
}