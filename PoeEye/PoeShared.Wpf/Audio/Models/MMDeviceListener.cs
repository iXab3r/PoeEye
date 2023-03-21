using System;
using System.Buffers;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using PoeShared.Scaffolding;

namespace PoeShared.Audio.Models;

public sealed class MMDeviceListener : DisposableReactiveObjectWithLogger
{
    private readonly WaveFormat captureFormat;
    private readonly ISubject<MMAudioBuffer> bufferSource = new Subject<MMAudioBuffer>();

    public MMDeviceListener(MMDevice device, WaveFormat captureFormat)
    {
        this.captureFormat = captureFormat;
        Device = device;

        var wasapiCapture = device.DataFlow == DataFlow.Capture ? new WasapiCapture(device) : new WasapiLoopbackCapture(device);
        var bufferedWaveInProvider = new BufferedWaveProvider(wasapiCapture.WaveFormat)
        {
            ReadFully = true,
            DiscardOnBufferOverflow = true
        };
        var resampler = BuildPipeline(bufferedWaveInProvider, bufferedWaveInProvider.WaveFormat, captureFormat, out var multiplier);

        MMAudioBuffer lastNotification = default;
        Observable.FromEventPattern<WaveInEventArgs>(h => wasapiCapture.DataAvailable += h, h => wasapiCapture.DataAvailable -= h)
            .Subscribe(x =>
            {
                bufferedWaveInProvider.AddSamples(x.EventArgs.Buffer, 0, x.EventArgs.BytesRecorded);

                var bytesToRead = (int) (x.EventArgs.BytesRecorded * multiplier);
                var buffer = new byte[bytesToRead];
                var converted = resampler.Read(buffer, 0, bytesToRead);
                var bufferNotification = new MMAudioBuffer(buffer, Stopwatch.GetElapsedTime(0), lastNotification.CaptureTimestamp);
                lastNotification = bufferNotification;
                bufferSource.OnNext(bufferNotification);
            })
            .AddTo(Anchors);

        wasapiCapture.StartRecording();

        if (device.DataFlow == DataFlow.Render)
        {
            var silenceProvider = new SilenceProvider(wasapiCapture.WaveFormat);
            var wasapiOut = new WasapiOut(device, AudioClientShareMode.Shared, false, 250);
            wasapiOut.Init(silenceProvider);
            wasapiOut.Play();
            Disposable.Create(() =>
            {
                if (wasapiOut.PlaybackState != PlaybackState.Stopped)
                {
                    wasapiOut.Stop();
                }
                wasapiOut.Dispose();
            }).AddTo(Anchors);
        }

        Buffers = bufferSource;
    }

    public MMDevice Device { get; }

    public IObservable<MMAudioBuffer> Buffers { get; }
    
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