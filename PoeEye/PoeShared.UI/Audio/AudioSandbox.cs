using System;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Threading;
using DynamicData;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using PoeShared.Audio.Models;
using PoeShared.Scaffolding;
using ReactiveUI;
using ScottPlot;
using ScottPlot.WPF;

namespace PoeShared.UI.Audio;

internal sealed class AudioSandbox : DisposableReactiveObjectWithLogger
{
    public AudioSandbox(IMMRenderDeviceProvider deviceProvider)
    {
        new[]
            {
                deviceProvider.Devices
                    .ToObservableChangeSet()
                    .Filter(x => !x.Equals(MMDeviceId.All)),
                new[] {MMDeviceId.DefaultInput, MMDeviceId.DefaultOutput}.ToSourceListEx().ToObservableChangeSet()
            }.Or()
            .ObserveOnDispatcher()
            .BindToCollection(out var devices)
            .Subscribe()
            .AddTo(Anchors);
        Devices = devices;
        DeviceId = MMDeviceId.DefaultOutput;

        var bufferDuration = TimeSpan.FromMilliseconds(100);
        var totalBufferDuration = TimeSpan.FromMilliseconds(1000);
        var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        var bufferBytesToRead = (int) (bufferDuration.TotalSeconds *  waveFormat.AverageBytesPerSecond);
        var totalBufferBytes = (int) (totalBufferDuration.TotalSeconds * waveFormat.AverageBytesPerSecond);
        var bytesPerSample = waveFormat.BitsPerSample / 8;
        AudioBuffer = new CircularBuffer<double>(new double[totalBufferBytes / bytesPerSample]);

        var bufferedWaveProvider = new BufferedWaveProvider(waveFormat)
        {
            DiscardOnBufferOverflow = true,
            ReadFully = true
        };

        this.WhenAnyValue(x => x.DeviceId)
            .Subscribe(x =>
            {
                if (DeviceListener != null)
                {
                    DeviceListener?.Dispose();
                }

                if (!x.IsEmpty)
                {
                    MMDevice device;
                    if (x.Equals(MMDeviceId.DefaultInput))
                    {
                        device = WasapiCapture.GetDefaultCaptureDevice();
                    } else if (x.Equals(MMDeviceId.DefaultOutput))
                    {
                        device = WasapiLoopbackCapture.GetDefaultLoopbackCaptureDevice();
                    }
                    else
                    {
                        device = deviceProvider.GetMixerControl(x.LineId);
                    }
                    DeviceListener = new MMDeviceListener(device, waveFormat);
                }
                else
                {
                    DeviceListener = null;
                }
            })
            .AddTo(Anchors);

        var dispatcher = Dispatcher.CurrentDispatcher;

        var fftSampleAggregator = new FftSampleAggregator()
        {
            PerformFFT = true,
            NotificationCount = AudioBuffer.Count
        };
        fftSampleAggregator.MaximumCalculated += FftSampleAggregatorOnMaximumCalculated;

        this.WhenAnyValue(x => x.DeviceListener)
            .Select(x => DeviceListener != null ? DeviceListener.Buffers : Observable.Empty<MMAudioBuffer>())
            .Switch()
            .Subscribe(x =>
            {
                Log.Debug(() => $"New buffer received: {x}");
                bufferedWaveProvider.AddSamples(x.Buffer, 0, x.BufferLength);

                if (bufferedWaveProvider.BufferedDuration > bufferDuration)
                {
                    var bytesToRead = new byte[bufferBytesToRead];
                    var bytesRead = bufferedWaveProvider.Read(bytesToRead, 0, bytesToRead.Length);
                    bufferedWaveProvider.ClearBuffer();

                    var samples = new float[bytesRead / bytesPerSample];
                    Buffer.BlockCopy(bytesToRead, 0, samples, 0, bytesRead);

                    samples.ForEach(fftSampleAggregator.Add);
                    samples.ForEach(x => AudioBuffer.PushBack(x));

                    var plot = Plot;
                    if (plot != null)
                    {
                        dispatcher.BeginInvoke(() =>
                        {
                            var currentLimits = plot.Plot.GetAxisLimits().Rect;

                            var level = AudioBuffer.Max();
                            var updatedLimits = new CoordinateRect(currentLimits.XMin, currentLimits.XMax, Math.Min(currentLimits.YMin, -level), Math.Max(currentLimits.YMax, level));
                            plot.Plot.SetAxisLimits(updatedLimits);
                            plot.Refresh();
                        });
                    }
                }
            })
            .AddTo(Anchors);

        ScatterBuffer = new Coordinates[2];
        this.WhenAnyValue(x => x.Plot)
            .Where(x => x != null)
            .Take(1)
            .Subscribe(x =>
            {
                var plot = x.Plot.Add.Signal(AudioBuffer, period: totalBufferDuration.TotalSeconds);
                var line = x.Plot.Add.Scatter(ScatterBuffer);
                line.LineStyle.Width = 2;
            })
            .AddTo(Anchors);

        this.WhenAnyValue(x => x.TargetLevel)
            .ObserveOnDispatcher()
            .Subscribe(x =>
            {
                ScatterBuffer[0] = new Coordinates(0, TargetLevel);
                ScatterBuffer[1] = new Coordinates(AudioBuffer.Count, TargetLevel);
                Plot?.Refresh();
            })
            .AddTo(Anchors);
    }

    private void FftSampleAggregatorOnMaximumCalculated(object sender, MaxSampleEventArgs e)
    {
        MinSample = e.MinSample;
        MaxSample = e.MaxSample;
        AllTimeMinSample = Math.Min(AllTimeMinSample, MinSample);
        AllTimeMaxSample = Math.Max(AllTimeMaxSample, MaxSample);
    }

    public float MinSample { get; private set; }

    public float MaxSample { get; private set; }
    
    public float AllTimeMinSample { get; private set; }
    
    public float AllTimeMaxSample { get; private set; }
    
    public float TargetLevel { get; set; }

    public IReadOnlyObservableCollection<MMDeviceId> Devices { get; }

    public MMDeviceId DeviceId { get; set; }

    public MMDeviceListener DeviceListener { get; private set; }

    public WpfPlot Plot { get; set; }

    public CircularBuffer<double> AudioBuffer { get; }
    
    public Coordinates[] ScatterBuffer { get; }
}