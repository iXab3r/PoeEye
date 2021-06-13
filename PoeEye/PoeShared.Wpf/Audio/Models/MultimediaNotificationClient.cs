using System;
using System.Reactive.Subjects;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace PoeShared.Audio.Models
{
    public sealed class MultimediaNotificationClient : IMMNotificationClient
    {
        private readonly ISubject<string> whenDeviceRemoved = new Subject<string>();
        private readonly ISubject<string> whenDeviceAdded = new Subject<string>();
        private readonly ISubject<(string deviceId, DeviceState newState)> whenDeviceStateChanged = new Subject<(string deviceId, DeviceState newState)>();
        private readonly ISubject<(string defaultDeviceId, DataFlow flow, Role role)> whenDefaultDeviceChanged = new Subject<(string defaultDeviceId, DataFlow flow, Role role)>();
        private readonly ISubject<(string pwstrDeviceId, PropertyKey key)> whenPropertyValueChanged = new Subject<(string pwstrDeviceId, PropertyKey key)>();

        public IObservable<string> WhenDeviceRemoved => whenDeviceRemoved;

        public IObservable<string> WhenDeviceAdded => whenDeviceAdded;

        public IObservable<(string deviceId, DeviceState newState)> WhenDeviceStateChanged => whenDeviceStateChanged;

        public IObservable<(string defaultDeviceId, DataFlow flow, Role role)> WhenDefaultDeviceChanged => whenDefaultDeviceChanged;

        public IObservable<(string pwstrDeviceId, PropertyKey key)> WhenPropertyValueChanged => whenPropertyValueChanged;

        public void OnDeviceStateChanged(string deviceId, DeviceState newState)
        {
            whenDeviceStateChanged.OnNext((deviceId: deviceId, newState: newState));
        }

        public void OnDeviceAdded(string pwstrDeviceId)
        {
            whenDeviceAdded.OnNext(pwstrDeviceId);
        }

        public void OnDeviceRemoved(string deviceId)
        {
            whenDeviceRemoved.OnNext(deviceId);
        }

        public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
        {
            whenDefaultDeviceChanged.OnNext((defaultDeviceId: defaultDeviceId, flow: flow, role: role));
        }

        public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
        {
            whenPropertyValueChanged.OnNext((pwstrDeviceId: pwstrDeviceId, key: key));
        }
    }
}