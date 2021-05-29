using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows.Input;
using DynamicData;
using DynamicData.Binding;
using JetBrains.Annotations;
using log4net;
using PoeShared.Audio.Services;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using Prism.Commands;
using ReactiveUI;
using Unity;

namespace PoeShared.Audio.ViewModels
{
    internal sealed class AudioNotificationSelectorViewModel : DisposableReactiveObject, IAudioNotificationSelectorViewModel
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AudioNotificationSelectorViewModel));
        private static readonly string DefaultNotification = AudioNotificationType.Whistle.ToString();
        private static readonly string DisabledNotification = AudioNotificationType.Disabled.ToString();
        
        private readonly IAudioNotificationsManager notificationsManager;
        private readonly ObservableAsPropertyHelper<string> previousSelectedValueSupplier;

        private bool audioEnabled;
        private string selectedValue;
        private float volume = 1;

        public AudioNotificationSelectorViewModel(
            IAudioNotificationsManager notificationsManager,
            [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(notificationsManager, nameof(notificationsManager));
            this.notificationsManager = notificationsManager;

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
                .Transform(x => x.ToLowerInvariant())
                .DistinctValues(x => x)
                .Sort(StringComparer.OrdinalIgnoreCase)
                .ToSourceList();

            new[] { 
                    new ObservableCollection<string>(preconfiguredNotifications).ToObservableChangeSet().ToSourceList(),
                    dynamicNotifications 
                }.ToSourceList()
                .Connect()
                .Transform(x => new NotificationTypeWrapperViewModel(this, x, x.Pascalize()))
                .DisposeMany()
                .Bind(out var notificationsSource)
                .SubscribeToErrors(Log.HandleUiException)
                .AddTo(Anchors);
            Items = notificationsSource;

            previousSelectedValueSupplier = this.WhenAnyValue(x => x.SelectedValue)
                .Where(x => !string.IsNullOrEmpty(SelectedValue) && !DisabledNotification.Equals(x, StringComparison.OrdinalIgnoreCase))
                .Select(x => selectedValue)
                .ToProperty(this, x => x.PreviousSelectedValue)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.AudioEnabled)
                .DistinctUntilChanged()
                .Where(x => x)
                .Where(x => DisabledNotification.Equals(selectedValue, StringComparison.OrdinalIgnoreCase))
                .SubscribeSafe(() => SelectedValue = PreviousSelectedValue ?? DefaultNotification, Log.HandleUiException)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.AudioEnabled)
                .DistinctUntilChanged()
                .Where(x => !x)
                .SubscribeSafe(() => SelectedValue = DisabledNotification, Log.HandleUiException)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.SelectedValue)
                .SubscribeSafe(x => AudioEnabled = !DisabledNotification.Equals(x, StringComparison.OrdinalIgnoreCase), Log.HandleUiException)
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

        public float Volume
        {
            get => volume;
            set => RaiseAndSetIfChanged(ref volume, value);
        }

        public string PreviousSelectedValue => previousSelectedValueSupplier.Value;

        public ReadOnlyObservableCollection<NotificationTypeWrapperViewModel> Items { get; }

        private void SelectNotificationCommandExecuted(object arg)
        {
            var notification = arg as NotificationTypeWrapperViewModel;
            if (notification == null)
            {
                return;
            }

            SelectedValue = notification.Value;
        }

        private void PlayNotificationCommandExecuted(object arg)
        {
            var notification = arg as NotificationTypeWrapperViewModel;
            if (notification == null)
            {
                return;
            }

            notificationsManager.PlayNotification(notification.Value, volume);
        }
    }
}