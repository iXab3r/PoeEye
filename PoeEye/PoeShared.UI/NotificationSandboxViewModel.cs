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

        public NotificationSandboxViewModel(INotificationsService notificationsService)
        {
            this.notificationsService = notificationsService;
            AddTextNotification = CommandWrapper.Create(AddTextNotificationExecuted);
            CloseAllNotifications = CommandWrapper.Create(notificationsService.CloseAll);
            NotificationImage = Assembly.GetExecutingAssembly().LoadBitmapFromResource("Resources\\giphy.gif");
        }

        public CommandWrapper AddTextNotification { get; }

        public CommandWrapper CloseAllNotifications { get; }

        public TimeSpan NotificationTimeout { get; set; } = TimeSpan.Zero;

        public string NotificationTitle { get; set; }

        public BitmapImage NotificationImage { get; set; }

        public string NotificationText { get; set; }

        public bool WithIcon { get; set; }

        public bool Interactive { get; set; } = true;

        public bool Closeable { get; set; }

        private void AddTextNotificationExecuted()
        {
            var rng = new Random();
            var notification = new TextNotificationViewModel()
            {
                Text = string.IsNullOrEmpty(NotificationText) ? Enumerable.Repeat("a", (int)rng.Next(10, 60)).JoinStrings(" ") : NotificationText,
                TimeToLive = NotificationTimeout,
                Title = NotificationTitle,
                Icon = WithIcon ? NotificationImage : default,
                Interactive = Interactive,
                Closeable = Closeable
            };

            notificationsService.AddNotification(notification);
        }
    }
}