using System;
using System.Windows.Media;
using PoeShared.Scaffolding;

namespace PoeShared.Notifications.ViewModels
{
    public abstract class NotificationViewModelBase : DisposableReactiveObject, INotificationViewModel
    {
        private string title;
        private ICloseController closeController;
        private ImageSource icon;
        private TimeSpan timeToLive;

        protected NotificationViewModelBase()
        {
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
    }
}