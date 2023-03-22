using System;
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

public sealed class MMDeviceListener : WaveInListener
{
    private readonly IWaveIn wasapiCapture;

    public MMDeviceListener(MMDeviceId deviceId, IMMDeviceProvider deviceProvider, WaveFormat captureFormat) : this(deviceProvider.GetDevice(deviceId), captureFormat)
    {
    }
    
    public MMDeviceListener(MMDevice device, WaveFormat outputFormat) : this(ToCaptureSession(device), outputFormat)
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
    
    public MMDeviceListener(IWaveIn wasapiCapture, WaveFormat outputFormat) : base(outputFormat)
    {
        this.wasapiCapture = wasapiCapture.AddTo(Anchors);
    }

    private static IWaveIn ToCaptureSession(MMDevice device)
    {
        return device.DataFlow == DataFlow.Capture ? new WasapiCapture(device) : new WasapiLoopbackCapture(device);
    }

    protected override IWaveIn GetSource()
    {
        return wasapiCapture;
    }
}