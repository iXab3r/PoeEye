using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using DynamicData;
using DynamicData.Binding;
using log4net;
using Microsoft.Win32;
using PoeShared.Audio.Services;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PoeShared.Scaffolding.WPF;
using Prism.Commands;
using ReactiveUI;

namespace PoeShared.Audio.ViewModels
{
    internal sealed class AudioNotificationSelectorViewModel : DisposableReactiveObject, IAudioNotificationSelectorViewModel
    {
        private static readonly IFluentLog Log = typeof(AudioNotificationSelectorViewModel).PrepareLogger();
        private static readonly string DefaultNotification = AudioNotificationType.Whistle.ToString();
        private static readonly string DisabledNotification = AudioNotificationType.Disabled.ToString();
        
        private readonly IAudioNotificationsManager notificationsManager;
        private readonly ObservableAsPropertyHelper<string> previousSelectedValueSupplier;

        public AudioNotificationSelectorViewModel(IAudioNotificationsManager notificationsManager)
        {
            Guard.ArgumentNotNull(notificationsManager, nameof(notificationsManager));
            this.notificationsManager = notificationsManager;

            SelectNotificationCommand = new DelegateCommand<object>(SelectNotificationCommandExecuted);
            PlayNotificationCommand = new DelegateCommand<object>(PlayNotificationCommandExecuted);
            AddSoundCommand = CommandWrapper.Create(AddSoundCommandExecuted);

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
                .Transform(x => new NotificationTypeWrapperViewModel(this, value: x, name: x.Pascalize()))
                .DisposeMany()
                .Bind(out var notificationsSource)
                .SubscribeToErrors(Log.HandleUiException)
                .AddTo(Anchors);
            Items = notificationsSource;

            this.WhenAnyValue(x => x.SelectedValue)
                .Where(x => !string.IsNullOrEmpty(SelectedValue) && !DisabledNotification.Equals(x, StringComparison.OrdinalIgnoreCase))
                .ToProperty(out previousSelectedValueSupplier, this, x => x.PreviousSelectedValue)
                .AddTo(Anchors);
            
            this.WhenAnyValue(x => x.SelectedValue)
                .SubscribeSafe(value => SelectedItem = Items.FirstOrDefault(x => string.Equals(value, x.Value, StringComparison.OrdinalIgnoreCase)), Log.HandleUiException)
                .AddTo(Anchors);
            
            this.WhenAnyValue(x => x.SelectedItem)
                .SubscribeSafe(x => SelectedValue = x?.Value ?? DisabledNotification, Log.HandleUiException)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.AudioEnabled)
                .DistinctUntilChanged()
                .Where(x => x)
                .Where(x => DisabledNotification.Equals(SelectedValue, StringComparison.OrdinalIgnoreCase))
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

        public bool AudioEnabled { get; set; }

        public ICommand SelectNotificationCommand { get; }

        public ICommand PlayNotificationCommand { get; }
        
        public ICommand AddSoundCommand { get; }

        public string LastOpenedDirectory { get; private set; }

        public float Volume { get; set; } = 1;
        
        public NotificationTypeWrapperViewModel SelectedItem { get; set; }

        public string SelectedValue { get; set; }

        public string PreviousSelectedValue => previousSelectedValueSupplier.Value;

        public ReadOnlyObservableCollection<NotificationTypeWrapperViewModel> Items { get; }

        private void SelectNotificationCommandExecuted(object arg)
        {
            switch (arg)
            {
                case NotificationTypeWrapperViewModel notificationTypeWrapper:
                    SelectedItem = notificationTypeWrapper;
                    break;
                case string value:
                    SelectedValue = value;
                    break;
                default:
                    throw new ArgumentException($"Unknown notification: {arg}");
            }
        }

        private void PlayNotificationCommandExecuted(object arg)
        {
            var notification = arg as NotificationTypeWrapperViewModel;
            if (notification == null)
            {
                return;
            }

            notificationsManager.PlayNotification(notification.Value, Volume);
        }
        
        private void AddSoundCommandExecuted()
        {
            Log.Info($"Showing OpenFileDialog to user");

            var op = new OpenFileDialog
            {
                Title = "Select an image", 
                InitialDirectory = !string.IsNullOrEmpty(LastOpenedDirectory) && Directory.Exists(LastOpenedDirectory) 
                    ? LastOpenedDirectory
                    : Environment.GetFolderPath(Environment.SpecialFolder.CommonMusic),
                CheckPathExists = true,
                Multiselect = false,
                Filter = "All supported sound files|*.wav;*.mp3|All files|*.*"
            };

            if (op.ShowDialog() != true)
            {
                return;
            }

            Log.Debug($"Adding notification {op.FileName}");
            LastOpenedDirectory = Path.GetDirectoryName(op.FileName);
            var notification = notificationsManager.AddFromFile(new FileInfo(op.FileName));
            SelectedValue = notification;
        }
    }
}