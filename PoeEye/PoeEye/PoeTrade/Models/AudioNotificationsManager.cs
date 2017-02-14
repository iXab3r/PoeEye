﻿using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using PoeEye.PoeTrade.Common;
using PoeEye.Resources.Notifications;
using PoeShared.Modularity;
using PoeEyeMainConfig = PoeEye.Config.PoeEyeMainConfig;

namespace PoeEye.PoeTrade.Models
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Media;
    using System.Reactive.Linq;

    using Config;

    using Guards;

    using JetBrains.Annotations;

    using PoeShared;
    using PoeShared.Scaffolding;

    using ReactiveUI;

    using IPoeEyeMainConfigProvider = IConfigProvider<PoeEyeMainConfig>;

    internal sealed class AudioNotificationsManager : DisposableReactiveObject, IAudioNotificationsManager
    {
        private readonly IDictionary<AudioNotificationType, byte[]> knownNotifications = new Dictionary
            <AudioNotificationType, byte[]>
        {
            {AudioNotificationType.Silence, new byte[0]},
        };

        private bool isEnabled;

        public AudioNotificationsManager([NotNull] IPoeEyeMainConfigProvider poeEyeConfigProvider) 
        {
            Guard.ArgumentNotNull(() => poeEyeConfigProvider);

            Log.Instance.Debug($"[AudioNotificationsManager..ctor] Initializing sound subsystem...");
            Initialize();

            var playNotificationCommandCanExecute = poeEyeConfigProvider
                .WhenAnyValue(x => x.ActualConfig)
                .Select(x => x.AudioNotificationsEnabled);

            var playNotificationCommand = new ReactiveCommand<AudioNotificationType>(
                playNotificationCommandCanExecute, x => Observable.Return((AudioNotificationType) x));
            playNotificationCommand
                .Where(x => x != AudioNotificationType.Disabled)
                .Subscribe(PlayNotification)
                .AddTo(Anchors);

            playNotificationCommandCanExecute
                .DistinctUntilChanged()
                .Subscribe(newValue => isEnabled = newValue)
                .AddTo(Anchors);
        }

        public void PlayNotification(AudioNotificationType notificationType)
        {
            Log.Instance.Debug($"[AudioNotificationsManager] Notification of type {notificationType} requested...");

            if (!isEnabled)
            {
                Log.Instance.Debug($"[AudioNotificationsManager] Playback is disabled ATM, skipping request");
                return;
            }

            byte[] notificationData;
            if (!knownNotifications.TryGetValue(notificationType, out notificationData))
            {
                Log.Instance.Warn(
                    $"[AudioNotificationsManager] Unknown notification type - {notificationType}, known notifications: {string.Join(", ", knownNotifications.Keys.Select(x => x.ToString()))}");
                return;
            }

            if (!notificationData.Any())
            {
                Log.Instance.Debug($"[AudioNotificationsManager] No sound data loaded for notification of type {notificationData}");
                return;
            }

            Log.Instance.Debug(
                $"[AudioNotificationsManager] Starting playback of {notificationType} ({notificationData.Length}b)...");
            using (var stream = new MemoryStream(notificationData))
            using (var notificationSound = new SoundPlayer(stream))
            {
                notificationSound.Play();
            }
        }

        private void Initialize()
        {
            Log.Instance.Debug(
                $"[AudioNotificationsManager.Initialize] Pre-defined notification list: {knownNotifications.Select(x => $"{x.Key} : {x.Value.Length}b")}");

            foreach (var notificationType in Enum.GetValues(typeof(AudioNotificationType)).Cast<AudioNotificationType>())
            {
                byte[] soundData;
                var notificationName = notificationType.ToString().ToLower();
                if (!SoundLibrary.TryToLoadSoundByName(notificationName, out soundData))
                {
                    Log.Instance.Warn($"[AudioNotificationsManager.Initialize] Failed to load notification {notificationType}");
                    continue;
                }
                knownNotifications.Add(notificationType, soundData);
            }

            Log.Instance.Debug(
                $"[AudioNotificationsManager.Initialize] Known notification list: {knownNotifications.Select(x => $"{x.Key} : {x.Value.Length}b")}");
        }
    }
}