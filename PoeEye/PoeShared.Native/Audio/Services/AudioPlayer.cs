using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Reactive.Disposables;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using PoeShared.Logging;
using PoeShared.Scaffolding; 

namespace PoeShared.Audio.Services;

[SupportedOSPlatform("windows")]
internal sealed class AudioPlayer : DisposableReactiveObject, IAudioPlayer
{
    private static readonly IFluentLog Log = typeof(AudioPlayer).PrepareLogger();

    public AudioPlayer()
    {
    }

    public IEnumerable<WaveOutDevice> GetDevices()
    {
        Log.Debug($"Retrieving MMDevices");

        using var deviceEnumerator = new MMDeviceEnumerator();
        var mmDevices =  deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
        try
        {
            Log.Debug($"Retrieving WaveOut devices, count: {WaveOut.DeviceCount}");
            var waveOutDevices = Enumerable.Range(1, WaveOut.DeviceCount).Select(idx =>
            {
                try
                {
                    var waveOutCapabilities = WaveOut.GetCapabilities(idx);
                    var matchingMmDevice = string.IsNullOrEmpty(waveOutCapabilities.ProductName) ? null : mmDevices.FirstOrDefault(x => x.FriendlyName?.StartsWith(waveOutCapabilities.ProductName) ?? false);
                    return new WaveOutDevice
                    {
                        DeviceNumber = idx,
                        WaveOutCapabilities = waveOutCapabilities,
                        MultimediaDeviceName = matchingMmDevice?.FriendlyName
                    };
                }
                catch (Exception e)
                {
                    Log.Warn($"Failed to get WaveOut capabilities for device #{idx}", e);
                    return null;
                }
            }).Where(x => x != null);
            
            return waveOutDevices;
        }
        finally
        {
            Log.Debug($"Releasing {mmDevices.Count} MMDevices");
            mmDevices.DisposeAll((device, ex) => Log.Warn($"Failed to dispose device { new { device, device.FriendlyName }}", ex));
        }
    }

    public Task Play(byte[] waveData)
    {
        return Play(waveData, volume: 1);
    }

    /// <summary>
    /// Plays specified wave data
    /// </summary>
    /// <param name="waveData">WAV data to play</param>
    /// <param name="volume">Volume, 1.0 is full scale</param>
    /// <returns></returns>
    public Task Play(byte[] waveData, float volume)
    {
        return PlayInternal(new AudioPlayerRequest() {WaveData = waveData, Volume = volume, CancellationToken = CancellationToken.None});
    }

    public Task Play(AudioPlayerRequest request)
    {
        return PlayInternal(request);
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

    private Task PlayInternal(AudioPlayerRequest request)
    {
        Log.Debug($"Queueing audio stream, request: {request}");
        return Task.Factory.StartNew(() =>
        {
            try
            {
                using (var rawStream = new MemoryStream(request.WaveData))
                using (var waveStream = new WaveFileReader(rawStream))
                using (WaveStream blockAlignedStream = new BlockAlignReductionStream(waveStream))
                using (var waveOut = new WaveOut(WaveCallbackInfo.FunctionCallback()))
                {
                    try
                    {
                        Log.Debug($"Initializing waveOut device {waveOut}, output: {request.OutputDevice}");
                        if (request.OutputDevice != null && request.OutputDevice != WaveOutDevice.DefaultDevice)
                        {
                            waveOut.DeviceNumber = request.OutputDevice.DeviceNumber;
                        }

                        waveOut.Init(blockAlignedStream);

                        var playbackAnchor = new ManualResetEvent(false);
                        EventHandler<StoppedEventArgs> playbackStoppedHandler = (sender, args) => { playbackAnchor.Set(); };
                        try
                        {
                            waveOut.PlaybackStopped += playbackStoppedHandler;
                            Log.Debug($"Starting to play audio stream({rawStream.Length}) using waveOut {waveOut}, volume: {request.Volume}...");
                            if (request.Volume != null)
                            {
                                waveOut.Volume = request.Volume.Value;
                            }

                            waveOut.Play();
                            WaitHandle.WaitAny(new[] {(WaitHandle) playbackAnchor, request.CancellationToken.WaitHandle});
                            if (request.CancellationToken.IsCancellationRequested)
                            {
                                Log.Debug($"Cancelling audio stream");
                                waveOut.Stop();
                                Log.Debug($"Stopped waveOut device");
                            }
                            else
                            {
                                Log.Debug(
                                    $"Successfully played audio stream({rawStream.Length}), token: {new {request.CancellationToken.IsCancellationRequested, request.CancellationToken.CanBeCanceled}}...");
                            }
                        }
                        finally
                        {
                            waveOut.PlaybackStopped -= playbackStoppedHandler;
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Exception in audio player, request: {request}", e);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"Failed to play audio stream, data: {request}", e);
            }
        }, request.CancellationToken);
    }
}