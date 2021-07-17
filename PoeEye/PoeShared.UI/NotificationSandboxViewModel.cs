using System;
using System.Linq;
using System.Reflection;
using System.Windows.Media.Imaging;
using PoeShared.Notifications.Services;
using PoeShared.Notifications.ViewModels;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;

namespace PoeShared.UI
{
    internal sealed class NotificationSandboxViewModel : DisposableReactiveObject
    {
        private readonly INotificationsService notificationsService;

        private BitmapImage notificationImage;

        private string notificationText;

        private TimeSpan notificationTimeout = TimeSpan.Zero;

        private string notificationTitle;

        private bool withIcon;

        public NotificationSandboxViewModel(INotificationsService notificationsService)
        {
            this.notificationsService = notificationsService;
            AddTextNotification = CommandWrapper.Create(AddTextNotificationExecuted);
            CloseAllNotifications = CommandWrapper.Create(notificationsService.CloseAll);
            NotificationImage = Assembly.GetExecutingAssembly().LoadBitmapFromResource("Resources\\giphy.gif");
        }

        public CommandWrapper AddTextNotification { get; }

        public CommandWrapper CloseAllNotifications { get; }

        public TimeSpan NotificationTimeout
        {
            get => notificationTimeout;
            set => RaiseAndSetIfChanged(ref notificationTimeout, value);
        }

        public string NotificationTitle
        {
            get => notificationTitle;
            set => RaiseAndSetIfChanged(ref notificationTitle, value);
        }

        public BitmapImage NotificationImage
        {
            get => notificationImage;
            set => RaiseAndSetIfChanged(ref notificationImage, value);
        }

        public string NotificationText
        {
            get => notificationText;
            set => RaiseAndSetIfChanged(ref notificationText, value);
        }

        public bool WithIcon
        {
            get => withIcon;
            set => RaiseAndSetIfChanged(ref withIcon, value);
        }

        private void AddTextNotificationExecuted()
        {
            var rng = new Random();
            var notification = new TextNotificationViewModel()
            {
                Text = string.IsNullOrEmpty(notificationText) ? Enumerable.Repeat("a", (int)rng.Next(10, 60)).JoinStrings(" ") : notificationText,
                TimeToLive = NotificationTimeout,
                Title = notificationTitle,
                Icon = withIcon ? NotificationImage : default,
            };

            notificationsService.AddNotification(notification);
        }
    }
}