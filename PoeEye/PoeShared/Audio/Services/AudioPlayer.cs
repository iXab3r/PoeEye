using System;
using System.IO;
using System.Media;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Threading;
using Common.Logging;
using JetBrains.Annotations;
using NAudio.Wave;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using Unity.Attributes;

namespace PoeShared.Audio.Services
{
    internal sealed class AudioPlayer : DisposableReactiveObject, IAudioPlayer
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AudioPlayer));

        private readonly IScheduler bgScheduler;

        public AudioPlayer([NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler)
        {
            this.bgScheduler = bgScheduler;
        }

        public IDisposable Play(Stream rawStream)
        {
            return PlayInternal(rawStream);
        }
        
        private IDisposable PlayInternalMedia(Stream rawStream)
        {
            using (var waveStream = WaveFormatConversionStream.CreatePcmStream(new WaveFileReader(rawStream)))
            using (var player = new SoundPlayer())
            {
                player.Stream = waveStream;
                player.Play();
            }

            return Disposable.Empty;
        }
        
        private IDisposable PlayInternal(Stream rawStream)
        {
            Log.Trace($"Queueing audio stream({rawStream.Length})...");
            return bgScheduler.Schedule(() =>
            {
                try
                {
                    using(var waveStream = new WaveFileReader(rawStream))
                    using (WaveStream blockAlignedStream = new BlockAlignReductionStream(waveStream))
                    using (WaveOut waveOut = new WaveOut(WaveCallbackInfo.FunctionCallback()))
                    using(rawStream)
                    {
                        {
                            Log.Trace($"Initializing waveOut device {waveOut}");
                            waveOut.Init(blockAlignedStream);

                            var playbackAnchor = new ManualResetEvent(false);
                            EventHandler<StoppedEventArgs> playbackStoppedHandler = (sender, args) => { playbackAnchor.Set(); };
                            try
                            {
                                waveOut.PlaybackStopped += playbackStoppedHandler;

                                Log.Trace($"Starting to play audio stream({rawStream.Length}) using waveOut {waveOut}...");
                                waveOut.Play();
                                
                                playbackAnchor.WaitOne();
                                Log.Trace($"Successfully played audio stream({rawStream.Length})...");
                            }
                            finally
                            {
                                waveOut.PlaybackStopped -= playbackStoppedHandler;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to play audio stream {rawStream}", e);
                }
            });
        }
    }
}