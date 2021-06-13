using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using log4net;
using NAudio.CoreAudioApi;
using NAudio.Utils;
using PoeShared.Scaffolding;

namespace PoeShared.Audio.Models
{
    internal sealed class MicrophoneProvider : DisposableReactiveObject, IMicrophoneProvider
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(MicrophoneProvider));
        private static readonly TimeSpan ThrottlingTimeout = TimeSpan.FromMilliseconds(100);
        private static readonly TimeSpan RetryTimeout = TimeSpan.FromSeconds(60);

        private readonly SourceList<MicrophoneLineData> microphoneLines = new SourceList<MicrophoneLineData>();
        private readonly MultimediaNotificationClient notificationClient = new MultimediaNotificationClient();
        private readonly MMDeviceEnumerator deviceEnumerator = new MMDeviceEnumerator();

        public MMDevice GetMixerControl(string lineId)
        {
            return EnumerateLinesInternal().FirstOrDefault(x => x.ID == lineId);
        }

        public MicrophoneProvider()
        {
            microphoneLines
                .Connect()
                .Bind(out var microphones)
                .SubscribeToErrors(Log.HandleUiException)
                .AddTo(Anchors);
            Microphones = microphones;

            Observable
                .Start(() =>
                {
                    Log.Debug($"Registering NotificationCallback using {deviceEnumerator}");
                    var hResult = deviceEnumerator.RegisterEndpointNotificationCallback(notificationClient);
                    if (hResult != HResult.S_OK)
                    {
                        throw new ApplicationException($"Failed to subscribe to Notifications using {deviceEnumerator}, hResult: {hResult}");
                    }
                    Log.Debug($"Successfully subscribed to Notifications using {deviceEnumerator}");
                })
                .RetryWithDelay(RetryTimeout)
                .SubscribeToErrors(Log.HandleUiException)
                .AddTo(Anchors);

            Observable.Merge(
                    Observable.Timer(DateTimeOffset.Now, RetryTimeout).ToUnit(),
                    notificationClient.WhenDeviceAdded.Do(deviceId => Log.Debug($"[Notification] Device added, id: {deviceId}")).ToUnit(),
                    notificationClient.WhenDeviceStateChanged.Do(x => Log.Debug($"[Notification] Device state changed, id: {x.deviceId}, state: {x.newState}")).ToUnit(),
                    notificationClient.WhenDeviceRemoved.Do(deviceId => Log.Debug($"[Notification] Device removed, id: {deviceId}")).ToUnit())
                .Throttle(ThrottlingTimeout)
                .Select(x => EnumerateLines())
                .DistinctUntilChanged(x => x.DumpToText())
                .SubscribeSafe(newLines =>
                {
                    Log.Debug($"Microphone lines list changed:\n\tCurrent lines list:\n\t\t{microphoneLines.Items.DumpToTable("\n\t\t")}\n\tNew lines list:\n\t\t{newLines.DumpToTable("\n\t\t")}");
                    var linesToAdd = newLines.Except(microphoneLines.Items).ToArray();
                    if (linesToAdd.Any())
                    {
                        Log.Debug($"Adding microphone lines: {linesToAdd.DumpToTextRaw()}");
                        microphoneLines.AddRange(linesToAdd);
                    }

                    var linesToRemove = microphoneLines.Items.Except(newLines).ToArray();
                    if (linesToRemove.Any())
                    {
                        Log.Debug($"Removing microphone lines: {linesToRemove.DumpToTextRaw()}");
                        microphoneLines.RemoveMany(linesToRemove);
                    }
                }, Log.HandleUiException)
                .AddTo(Anchors);
        }

        public ReadOnlyObservableCollection<MicrophoneLineData> Microphones { get; }

        private IEnumerable<MicrophoneLineData> EnumerateLines()
        {
            yield return MicrophoneLineData.All;

            var devices = EnumerateLinesInternal();
            foreach (var device in devices)
            {
                yield return new MicrophoneLineData(lineId: device.ID, name: device.FriendlyName);
            }
        }

        private IEnumerable<MMDevice> EnumerateLinesInternal()
        {
            var devices = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
            return devices;
        }
    }
}