﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using DynamicData;
using DynamicData.Binding;
using JetBrains.Annotations;
using PoeShared.Audio.Services;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using Prism.Commands;
using ReactiveUI;

namespace PoeShared.Audio.ViewModels
{
    internal sealed class AudioNotificationSelectorViewModel : DisposableReactiveObject,
        IAudioNotificationSelectorViewModel
    {
        private static readonly string DefaultNotification = AudioNotificationType.Whistle.ToString();
        private readonly IAudioNotificationsManager notificationsManager;
        private bool audioEnabled;
        private string selectedValue;

        public AudioNotificationSelectorViewModel([NotNull] IAudioNotificationsManager notificationsManager)
        {
            this.notificationsManager = notificationsManager;
            Guard.ArgumentNotNull(notificationsManager, nameof(notificationsManager));

            SelectNotificationCommand = new DelegateCommand<object>(SelectNotificationCommandExecuted);
            PlayNotificationCommand = new DelegateCommand<object>(PlayNotificationCommandExecuted);

            var preconfiguredNotifications = new[]
            {
                AudioNotificationType.Disabled,
                AudioNotificationType.Silence
            }.Select(x => x.ToString()).ToArray();

            var dynamicNotifications = new SourceList<string>()
                .Connect()
                .Or(
                    notificationsManager.Notifications.ToObservableChangeSet())
                .Sort(StringComparer.OrdinalIgnoreCase);

            new SourceList<string>()
                .Connect()
                .Or(
                    new ObservableCollection<string>(preconfiguredNotifications).ToObservableChangeSet(),
                    dynamicNotifications)
                .DistinctValues(x => x)
                .Transform(x => (object) new NotificationTypeWrapper(this, x, x.Pascalize()))
                .Bind(out var notificationsSource)
                .Subscribe()
                .AddTo(Anchors);
            Items = notificationsSource;

            this.WhenAnyValue(x => x.AudioEnabled)
                .DistinctUntilChanged()
                .Where(x => x)
                .Where(x => selectedValue == AudioNotificationType.Disabled.ToString())
                .Subscribe(() => SelectedValue = DefaultNotification)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.AudioEnabled)
                .DistinctUntilChanged()
                .Where(x => !x)
                .Subscribe(() => SelectedValue = AudioNotificationType.Disabled.ToString())
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.SelectedValue)
                .Subscribe(x => AudioEnabled = x != AudioNotificationType.Disabled.ToString())
                .AddTo(Anchors);
        }

        public bool AudioEnabled
        {
            get => audioEnabled;
            set => this.RaiseAndSetIfChanged(ref audioEnabled, value);
        }

        public ICommand SelectNotificationCommand { get; }

        public ICommand PlayNotificationCommand { get; }

        public string SelectedValue
        {
            get => selectedValue;
            set => this.RaiseAndSetIfChanged(ref selectedValue, value);
        }

        public AudioNotificationType SelectedItem
        {
            get => SelectedValue.ParseEnumSafe<AudioNotificationType>();
            set => SelectedValue = value.ToString();
        }

        public ReadOnlyObservableCollection<object> Items { get; }

        private void SelectNotificationCommandExecuted(object arg)
        {
            var notification = arg as NotificationTypeWrapper;
            if (notification == null)
            {
                return;
            }

            SelectedValue = notification.Value;
        }

        private void PlayNotificationCommandExecuted(object arg)
        {
            var notification = arg as NotificationTypeWrapper;
            if (notification == null)
            {
                return;
            }

            notificationsManager.PlayNotification(notification.Value);
        }

        public sealed class NotificationTypeWrapper : ReactiveObject
        {
            private readonly AudioNotificationSelectorViewModel owner;

            public NotificationTypeWrapper(
                AudioNotificationSelectorViewModel owner,
                string value,
                string name)
            {
                this.owner = owner;
                Value = value;
                Name = name;

                this.owner
                    .WhenAnyValue(x => x.SelectedValue)
                    .Subscribe(() => this.RaisePropertyChanged(nameof(IsSelected)))
                    .AddTo(this.owner.Anchors);
            }

            public bool IsSelected => string.Compare(owner.SelectedValue, Value, StringComparison.OrdinalIgnoreCase) == 0;

            public string Value { get; }
            
            public ICommand PlayNotificationCommand => owner.PlayNotificationCommand;

            public ICommand SelectNotificationCommand => owner.SelectNotificationCommand;

            public string Name { get; }
        }
    }
}