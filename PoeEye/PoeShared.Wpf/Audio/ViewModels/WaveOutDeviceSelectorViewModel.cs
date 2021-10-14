using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DynamicData;
using log4net;
using NAudio.CoreAudioApi;
using NAudio.Utils;
using PoeShared.Audio.Models;
using PoeShared.Audio.Services;
using PoeShared.Prism;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using ReactiveUI;
using Unity;

namespace PoeShared.Audio.ViewModels
{
    internal sealed class WaveOutDeviceSelectorViewModel : DisposableReactiveObject, IWaveOutDeviceSelectorViewModel
    {
        private static readonly IFluentLog Log = typeof(WaveOutDeviceSelectorViewModel).PrepareLogger();
        
        private static readonly TimeSpan ThrottlingTimeout = TimeSpan.FromMilliseconds(100);
        private static readonly TimeSpan RetryTimeout = TimeSpan.FromSeconds(60);

        private readonly IAudioPlayer audioPlayer;
        private readonly ISourceCache<WaveOutDevice, string> devicesSource = new SourceCache<WaveOutDevice, string>(x => x.Id);
        private readonly MultimediaNotificationClient notificationClient = new MultimediaNotificationClient();
        private readonly MMDeviceEnumerator deviceEnumerator;

        public WaveOutDeviceSelectorViewModel(
            IAudioPlayer audioPlayer,
            [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            this.audioPlayer = audioPlayer;
            devicesSource
                .Connect()
                .OnItemAdded(x => Log.Debug($"Added WaveOut device: {x}"))
                .OnItemRemoved(x => Log.Debug($"Removed WaveOut device: {x}"))
                .OnItemUpdated((prev, curr) => Log.Debug($"Updated WaveOut device, previous: {prev}, current: {curr}"))
                .ObserveOn(uiScheduler)
                .Bind(out var devices)
                .SubscribeToErrors(Log.HandleException)
                .AddTo(Anchors);
            Devices = devices;
            
            Observable.Merge(
                    Observable.Timer(DateTimeOffset.Now, RetryTimeout).ToUnit(),
                    notificationClient.WhenDeviceAdded.Do(deviceId => Log.Debug($"[Notification] Device added, id: {deviceId}")).ToUnit(),
                    notificationClient.WhenDeviceStateChanged.Do(x => Log.Debug($"[Notification] Device state changed, id: {x.deviceId}, state: {x.newState}")).ToUnit(),
                    notificationClient.WhenDeviceRemoved.Do(deviceId => Log.Debug($"[Notification] Device removed, id: {deviceId}")).ToUnit())
                .Throttle(ThrottlingTimeout)
                .RetryWithDelay(RetryTimeout)
                .SubscribeSafe(HandleDevicesUpdate, Log.HandleException)
                .AddTo(Anchors);
            
            deviceEnumerator = new MMDeviceEnumerator().AddTo(Anchors);
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
                .SubscribeToErrors(Log.HandleException)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.SelectedItem)
                .Where(x => x == null)
                .SubscribeSafe(_ => SelectedItem = WaveOutDevice.DefaultDevice, Log.HandleException)
                .AddTo(Anchors);
        }

        public WaveOutDevice SelectedItem { get; set; }

        public ReadOnlyObservableCollection<WaveOutDevice> Devices { get; }
        
        public void SelectById(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId) || SelectedItem?.Id == deviceId)
            {
                return;
            }
            Log.Debug($"Selecting item by id {deviceId} out of {devicesSource.Count}");
            var device = devicesSource.Lookup(deviceId);
            Log.Debug($"Found device {device}");
            SelectedItem = device.HasValue ? device.Value : null;
        }

        private void HandleDevicesUpdate()
        {
            var devices = new[] {WaveOutDevice.DefaultDevice}.Concat(audioPlayer.GetDevices())
                .ToDictionary(x => x.Id, x => x, tuple =>
                {
                    Log.Warn($"Conflict between devices with Id {tuple.key} - ignoring new device, existing device: {tuple.existingValue}, new device: {tuple.newValue}");
                    return tuple.existingValue;
                });
            var devicesToRemove = devicesSource.Items.Where(x => !devices.ContainsKey(x.Id)).ToArray();
            if (devicesToRemove.Any())
            {
                devicesSource.RemoveKeys(devicesToRemove.Select(x => x.Id));
            }
            devicesSource.AddOrUpdateIfNeeded(devices.Values);
        }
    }
}