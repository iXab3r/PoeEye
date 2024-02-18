using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace PoeShared.Audio.Services;

public interface IAudioPlayer : IDisposableReactiveObject
{
    IEnumerable<WaveOutDevice> GetDevices();
        
    [NotNull]
    Task Play([NotNull] byte[] waveData, CancellationToken cancellationToken = default);
        
    [NotNull]
    Task Play([NotNull] byte[] waveData, float volume, CancellationToken cancellationToken = default);
    
    Task Play(AudioPlayerRequest request, CancellationToken cancellationToken = default);
}