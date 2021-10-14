using System;
using System.Windows.Media;
using PoeShared.Scaffolding;
using PoeShared.Services;

namespace PoeShared.Notifications.ViewModels
{
    public abstract class NotificationViewModelBase : DisposableReactiveObject, INotificationViewModel
    {
        private bool closeable = true;

        private bool interactive = true;

        protected NotificationViewModelBase()
        {
        }

        public bool Interactive
        {
            get => interactive;
            set => RaiseAndSetIfChanged(ref interactive, value);
        }

        public ICloseController CloseController { get; set; }

        public string Title { get; set; }

        public ImageSource Icon { get; set; }

        public TimeSpan TimeToLive { get; set; }

        public bool Closeable
        {
            get => closeable;
            set => RaiseAndSetIfChanged(ref closeable, value);
        }
    }
}