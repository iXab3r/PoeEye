using System;
using System.Windows.Media;
using PoeShared.Scaffolding;

namespace PoeShared.Notifications.ViewModels
{
    public abstract class NotificationViewModelBase : DisposableReactiveObject, INotificationViewModel
    {
        private bool closeable = true;
        private ICloseController closeController;
        private ImageSource icon;

        private bool interactive = true;
        private TimeSpan timeToLive;
        private string title;

        protected NotificationViewModelBase()
        {
        }

        public bool Interactive
        {
            get => interactive;
            set => RaiseAndSetIfChanged(ref interactive, value);
        }

        public ICloseController CloseController
        {
            get => closeController;
            set => RaiseAndSetIfChanged(ref closeController, value);
        }

        public string Title
        {
            get => title;
            set => RaiseAndSetIfChanged(ref title, value);
        }

        public ImageSource Icon
        {
            get => icon;
            set => RaiseAndSetIfChanged(ref icon, value);
        }

        public TimeSpan TimeToLive
        {
            get => timeToLive;
            set => RaiseAndSetIfChanged(ref timeToLive, value);
        }

        public bool Closeable
        {
            get => closeable;
            set => RaiseAndSetIfChanged(ref closeable, value);
        }
    }
}