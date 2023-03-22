using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using NAudio.Wave;
using PoeShared.Scaffolding;

namespace PoeShared.Audio.Models;

public abstract class WaveInListener : DisposableReactiveObjectWithLogger, IAudioListener
{
    public WaveFormat OutputFormat { get; }
    
    public IObservable<MMAudioBuffer> Buffers => SubscribeToBuffers();

    protected WaveInListener(WaveFormat outputFormat)
    {
        OutputFormat = outputFormat;
    }

    protected abstract IWaveIn GetSource();

    private IObservable<MMAudioBuffer> SubscribeToBuffers()
    {
        return Observable.Create<MMAudioBuffer>(observer =>
        {
            var anchors = new CompositeDisposable();
            var audioSource = GetSource();
            
            var bufferedWaveInProvider = new BufferedWaveProvider(audioSource.WaveFormat)
            {
                ReadFully = true,
                DiscardOnBufferOverflow = true
            };
            var resampler = BuildPipeline(bufferedWaveInProvider, bufferedWaveInProvider.WaveFormat, OutputFormat, out var multiplier);

            MMAudioBuffer lastNotification = default;
            Observable.FromEventPattern<WaveInEventArgs>(h => audioSource.DataAvailable += h, h => audioSource.DataAvailable -= h)
                .Select(x => x.EventArgs)
                .Select(args =>
                {
                    bufferedWaveInProvider.AddSamples(args.Buffer, 0, args.BytesRecorded);

                    var bytesToRead = (int) (args.BytesRecorded * multiplier);
                    var buffer = new byte[bytesToRead];
                    var converted = resampler.Read(buffer, 0, bytesToRead);
                    var bufferNotification = new MMAudioBuffer(buffer, Stopwatch.GetElapsedTime(0), lastNotification.CaptureTimestamp);

                    return lastNotification = bufferNotification;
                })
                .Subscribe(observer.OnNext)
                .AddTo(anchors);
            
            audioSource.StartRecording();
            Disposable.Create(() => audioSource.StopRecording()).AddTo(anchors);
            
            return anchors;
        });
    }
    
    private static IWaveProvider BuildPipeline(
        IWaveProvider provider, 
        WaveFormat input,
        WaveFormat output, 
        out float multiplier)
    {
        multiplier = 1.0f;
        if (input.Channels == output.Channels && input.SampleRate == output.SampleRate)
        {
            return provider;
        }

        provider = new MediaFoundationResampler(provider, output);
        multiplier *= (float) input.SampleRate / output.SampleRate;
        multiplier *= (float) input.Channels / output.Channels;

        multiplier = 1.0f / multiplier;
        return provider;
    }
}