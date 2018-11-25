using System;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using Guards;
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

            Items = new ReactiveList<object>();

            SelectNotificationCommand = new DelegateCommand<object>(SelectNotificationCommandExecuted);
            PlayNotificationCommand = new DelegateCommand<object>(PlayNotificationCommandExecuted);

            var preconfiguredNotifications = new[]
            {
                AudioNotificationType.Disabled,
                AudioNotificationType.Silence,
            }.Select(x => x.ToString());

            var defaultNotifications = new[] { 
                AudioNotificationType.Whistle,
                AudioNotificationType.Bell,
                AudioNotificationType.Mercury,
                AudioNotificationType.DingDong,
                AudioNotificationType.Ping,
                AudioNotificationType.Minions,
                AudioNotificationType.Wob
            }.Select(x => x.ToString());

            var notifications = defaultNotifications.Concat(notificationsManager.Notifications)
                .Select(x => x.Pascalize())
                .Distinct()
                .OrderBy(x => x);
            
            Items.AddRange(
                preconfiguredNotifications
                    .Concat(notifications)
                    .Select(x => new NotificationTypeWrapper(this, x, x.ToString())));

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

        public IReactiveList<object> Items { get; }

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
                    .Subscribe(() => this.RaisePropertyChanged(nameof(IsSelected)));
            }

            public bool IsSelected => owner.SelectedValue == Value;

            public string Value { get; }

            public ICommand PlayNotificationCommand => owner.PlayNotificationCommand;

            public ICommand SelectNotificationCommand => owner.SelectNotificationCommand;

            public string Name { get; }
        }
    }
}