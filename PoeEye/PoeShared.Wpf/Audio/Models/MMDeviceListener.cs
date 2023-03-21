using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using PoeShared.Scaffolding;
using AudioClientShareMode = NAudio.CoreAudioApi.AudioClientShareMode;
using DataFlow = NAudio.CoreAudioApi.DataFlow;
using MMDevice = NAudio.CoreAudioApi.MMDevice;

namespace PoeShared.Audio.Models;

public sealed class MMDeviceListener : DisposableReactiveObjectWithLogger
{
    private readonly IWaveIn wasapiCapture;
    private readonly WaveFormat captureFormat;
    private readonly ISubject<MMAudioBuffer> bufferSource = new Subject<MMAudioBuffer>();

    public MMDeviceListener(MMDevice device, WaveFormat captureFormat) : this(ToCaptureSession(device), captureFormat)
    {
        if (device.DataFlow != DataFlow.Render)
        {
            return;
        }

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
    
    public MMDeviceListener(IWaveIn wasapiCapture, WaveFormat captureFormat)
    {
        this.wasapiCapture = wasapiCapture.AddTo(Anchors);
        this.captureFormat = captureFormat;

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


        Buffers = bufferSource;
    }

    public IObservable<MMAudioBuffer> Buffers { get; }

    private static IWaveIn ToCaptureSession(MMDevice device)
    {
        return device.DataFlow == DataFlow.Capture ? new WasapiCapture(device) : new WasapiLoopbackCapture(device);
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