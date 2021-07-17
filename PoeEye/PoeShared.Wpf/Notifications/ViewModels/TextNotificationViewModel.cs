namespace PoeShared.Notifications.ViewModels
{
    public sealed class TextNotificationViewModel : NotificationViewModelBase
    {
        private string text;

        public string Text
        {
            get => text;
            set => RaiseAndSetIfChanged(ref text, value);
        }
    }
}