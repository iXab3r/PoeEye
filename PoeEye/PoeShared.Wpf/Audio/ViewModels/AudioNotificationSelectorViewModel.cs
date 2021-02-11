using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using DynamicData;
using DynamicData.Binding;
using JetBrains.Annotations;
using log4net;
using PoeShared.Audio.Services;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using Prism.Commands;
using ReactiveUI;

namespace PoeShared.Audio.ViewModels
{
    internal sealed class AudioNotificationSelectorViewModel : DisposableReactiveObject, IAudioNotificationSelectorViewModel
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AudioNotificationSelectorViewModel));

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

            var defaultNotifications = new[]
            {
                AudioNotificationType.Whistle,
                AudioNotificationType.Bell,
                AudioNotificationType.Mercury,
                AudioNotificationType.DingDong,
                AudioNotificationType.Ping,
                AudioNotificationType.Minions,
                AudioNotificationType.Wob
            }.Select(x => x.ToString()).ToArray();

            var dynamicNotifications = new SourceList<string>()
                .Connect()
                .Or(
                    new ObservableCollection<string>(defaultNotifications).ToObservableChangeSet(),
                    notificationsManager.Notifications.ToObservableChangeSet())
                .Sort(StringComparer.OrdinalIgnoreCase);

            new SourceList<string>()
                .Connect()
                .Or(
                    new ObservableCollection<string>(preconfiguredNotifications).ToObservableChangeSet(),
                    dynamicNotifications)
                .Distinct()
                .Transform(x => (object) new NotificationTypeWrapper(this, x, x.Pascalize()))
                .Bind(out var notificationsSource)
                .SubscribeToErrors(Log.HandleUiException)
                .AddTo(Anchors);
            Items = notificationsSource;

            this.WhenAnyValue(x => x.AudioEnabled)
                .DistinctUntilChanged()
                .Where(x => x)
                .Where(x => selectedValue == AudioNotificationType.Disabled.ToString())
                .SubscribeSafe(() => SelectedValue = DefaultNotification, Log.HandleUiException)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.AudioEnabled)
                .DistinctUntilChanged()
                .Where(x => !x)
                .SubscribeSafe(() => SelectedValue = AudioNotificationType.Disabled.ToString(), Log.HandleUiException)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.SelectedValue)
                .SubscribeSafe(x => AudioEnabled = x != AudioNotificationType.Disabled.ToString(), Log.HandleUiException)
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
                    .SubscribeSafe(() => this.RaisePropertyChanged(nameof(IsSelected)), Log.HandleUiException);
            }

            public bool IsSelected => owner.SelectedValue == Value;

            public string Value { get; }
            
            public ICommand PlayNotificationCommand => owner.PlayNotificationCommand;

            public ICommand SelectNotificationCommand => owner.SelectNotificationCommand;

            public string Name { get; }
        }
    }
}