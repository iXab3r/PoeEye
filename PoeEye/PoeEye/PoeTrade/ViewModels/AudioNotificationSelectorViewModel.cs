using System;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using CsQuery.ExtensionMethods;
using Guards;
using JetBrains.Annotations;
using PoeEye.PoeTrade.Common;
using PoeEye.PoeTrade.Models;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeEye.PoeTrade.ViewModels
{
    internal class AudioNotificationSelectorViewModel : DisposableReactiveObject, IDisposableReactiveObject,
        IAudioNotificationSelectorViewModel
    {
        private readonly IAudioNotificationsManager notificationsManager;
        private bool audioEnabled;
        private AudioNotificationType selectedValue;

        private const AudioNotificationType DefaultNotification = AudioNotificationType.Whistle;

        public AudioNotificationSelectorViewModel([NotNull] IAudioNotificationsManager notificationsManager)
        {
            this.notificationsManager = notificationsManager;
            Guard.ArgumentNotNull(() => notificationsManager);

            Items = new ReactiveList<object>();

            var selectNotificationCommand = ReactiveCommand.Create();
            selectNotificationCommand.Subscribe(SelectNotificationCommandExecuted).AddTo(Anchors);
            SelectNotificationCommand = selectNotificationCommand;

            var playNotificationCommand = ReactiveCommand.Create();
            playNotificationCommand.Subscribe(PlayNotificationCommandExecuted).AddTo(Anchors);
            PlayNotificationCommand = playNotificationCommand;

            Items.AddRange(new[]
            {
                AudioNotificationType.Disabled,
                AudioNotificationType.Silence,
                AudioNotificationType.Whistle,
                AudioNotificationType.Bell,
                AudioNotificationType.DingDong,
                AudioNotificationType.Ping,
                AudioNotificationType.Minions,
                AudioNotificationType.Wob,
            }.Select(x => new NotificationTypeWrapper(this, x, x.ToString())));

            this.WhenAnyValue(x => x.AudioEnabled)
                .DistinctUntilChanged()
                .Where(x => x)
                .Where(x => selectedValue == AudioNotificationType.Disabled)
                .Subscribe(() => SelectedValue = DefaultNotification)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.AudioEnabled)
              .DistinctUntilChanged()
              .Where(x => !x)
              .Subscribe(() => SelectedValue = AudioNotificationType.Disabled)
              .AddTo(Anchors);

            this.WhenAnyValue(x => x.SelectedValue)
                .Subscribe(x => AudioEnabled = x != AudioNotificationType.Disabled)
                .AddTo(Anchors);
        }

        public bool AudioEnabled
        {
            get { return audioEnabled; }
            set { this.RaiseAndSetIfChanged(ref audioEnabled, value); }
        }

        public AudioNotificationType SelectedValue
        {
            get { return selectedValue; }
            set { this.RaiseAndSetIfChanged(ref selectedValue, value); }
        }

        public IReactiveList<object> Items { get; }

        internal ICommand SelectNotificationCommand { get; }

        internal ICommand PlayNotificationCommand { get; }

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
                AudioNotificationType value,
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

            public AudioNotificationType Value { get; }

            public ICommand PlayNotificationCommand => owner.PlayNotificationCommand;

            public ICommand SelectNotificationCommand => owner.SelectNotificationCommand;

            public string Name { get; }
        }
    }
}