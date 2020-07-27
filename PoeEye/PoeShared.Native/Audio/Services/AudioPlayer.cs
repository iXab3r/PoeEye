using System;
using System.IO;
using System.Media;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using log4net;
using NAudio.Wave;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using Unity;

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

        public Task Play(byte[] waveData)
        {
            return PlayInternal(waveData);
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

        private Task PlayInternal(byte[] soundData)
        {
            Log.Debug($"Queueing audio stream({soundData.Length})...");
            return Task.Factory.StartNew(() =>
            {
                try
                {
                    using (var rawStream = new MemoryStream(soundData))
                    using (var waveStream = new WaveFileReader(rawStream))
                    using (WaveStream blockAlignedStream = new BlockAlignReductionStream(waveStream))
                    using (var waveOut = new WaveOut(WaveCallbackInfo.FunctionCallback()))
                    {
                        {
                            Log.Debug($"Initializing waveOut device {waveOut}");
                            waveOut.Init(blockAlignedStream);

                            var playbackAnchor = new ManualResetEvent(false);
                            EventHandler<StoppedEventArgs> playbackStoppedHandler = (sender, args) => { playbackAnchor.Set(); };
                            try
                            {
                                waveOut.PlaybackStopped += playbackStoppedHandler;

                                Log.Debug($"Starting to play audio stream({rawStream.Length}) using waveOut {waveOut}...");
                                waveOut.Play();

                                playbackAnchor.WaitOne();
                                Log.Debug($"Successfully played audio stream({rawStream.Length})...");
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
                    Log.Error($"Failed to play audio stream of length {soundData.Length}b", e);
                }
            });
        }
    }
}