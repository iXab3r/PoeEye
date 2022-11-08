using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using NAudio.CoreAudioApi;
using NAudio.Utils;
using PoeShared.Scaffolding; 

namespace PoeShared.Audio.Models;

internal abstract class MMDeviceProviderBase : DisposableReactiveObjectWithLogger, IMMDeviceProvider
{
    private static readonly TimeSpan ThrottlingTimeout = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan RetryTimeout = TimeSpan.FromSeconds(60);

    private readonly SourceListEx<MMDeviceId> microphoneLines = new();
    private readonly MultimediaNotificationClient notificationClient = new();
    private readonly MMDeviceEnumerator deviceEnumerator;

    public MMDevice GetMixerControl(string lineId)
    {
        var lines = EnumerateLinesInternal();
        var result = lines.FirstOrDefault(x => x.ID == lineId);
        lines.Except(result == null ? Array.Empty<MMDevice>() : new[] { result }).DisposeAll((device, ex) => Log.Warn($"Failed to dispose device { new { device, device.FriendlyName }}", ex));
        return result;
    }

    public MMDeviceProviderBase(DataFlow dataFlow)
    {
        DataFlow = dataFlow;
        deviceEnumerator = new MMDeviceEnumerator().AddTo(Anchors);
        microphoneLines
            .Connect()
            .BindToCollection(out var microphones)
            .SubscribeToErrors(Log.HandleUiException)
            .AddTo(Anchors);
        Devices = microphones;

        Observable
            .Start(() =>
            {
                Log.Debug(() => $"Registering NotificationCallback using {deviceEnumerator}");
                var hResult = deviceEnumerator.RegisterEndpointNotificationCallback(notificationClient);
                if (hResult != HResult.S_OK)
                {
                    throw new ApplicationException($"Failed to subscribe to Notifications using {deviceEnumerator}, hResult: {hResult}");
                }
                Log.Debug(() => $"Successfully subscribed to Notifications using {deviceEnumerator}");
            })
            .RetryWithDelay(RetryTimeout)
            .SubscribeToErrors(Log.HandleUiException)
            .AddTo(Anchors);

        Observable.Merge(
                Observables.BlockingTimer(RetryTimeout).ToUnit(),
                notificationClient.WhenDeviceAdded.Do(deviceId => Log.Debug(() => $"[Notification] Device added, id: {deviceId}")).ToUnit(),
                notificationClient.WhenDeviceStateChanged.Do(x => Log.Debug(() => $"[Notification] Device state changed, id: {x.deviceId}, state: {x.newState}")).ToUnit(),
                notificationClient.WhenDeviceRemoved.Do(deviceId => Log.Debug(() => $"[Notification] Device removed, id: {deviceId}")).ToUnit())
            .Throttle(ThrottlingTimeout)
            .StartWithDefault()
            .Select(x => EnumerateLines())
            .DistinctUntilChanged(x => x.Dump())
            .SubscribeSafe(newLines =>
            {
                Log.Debug(() => $"Microphone lines list changed:\n\tCurrent lines list:\n\t\t{microphoneLines.Items.DumpToTable("\n\t\t")}\n\tNew lines list:\n\t\t{newLines.DumpToTable("\n\t\t")}");
                var linesToAdd = newLines.Except(microphoneLines.Items).ToArray();
                if (linesToAdd.Any())
                {
                    Log.Debug(() => $"Adding microphone lines: {linesToAdd.Dump()}");
                    microphoneLines.AddRange(linesToAdd);
                }

                var linesToRemove = microphoneLines.Items.Except(newLines).ToArray();
                if (linesToRemove.Any())
                {
                    Log.Debug(() => $"Removing microphone lines: {linesToRemove.Dump()}");
                    microphoneLines.RemoveMany(linesToRemove);
                }
            }, Log.HandleUiException)
            .AddTo(Anchors);
    }

    public IReadOnlyObservableCollection<MMDeviceId> Devices { get; }
    
    public DataFlow DataFlow { get; }
    
    private IEnumerable<MMDeviceId> EnumerateLines()
    {
        yield return MMDeviceId.All;

        var devices = EnumerateLinesInternal();
        try
        {
            foreach (var device in devices)
            {
                yield return new MMDeviceId(lineId: device.ID, name: device.FriendlyName);
            }
        }
        finally
        {
            devices.DisposeAll((device, ex) => Log.Warn($"Failed to dispose device { new { device, device.FriendlyName }}", ex));
        }
    }

    private MMDevice[] EnumerateLinesInternal()
    {
        return deviceEnumerator.EnumerateAudioEndPoints(DataFlow, DeviceState.Active).ToArray();
    }
}