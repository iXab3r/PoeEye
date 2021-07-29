using PoeShared.Notifications.ViewModels;

namespace PoeShared.UI
{
    internal sealed class RecordingNotificationViewModel : NotificationViewModelBase
    {
        public RecordingNotificationViewModel(IHotkeySequenceEditorController controller)
        {
            Controller = controller;
            Closeable = false;
            Interactive = false;
        }

        public IHotkeySequenceEditorController Controller { get; }
    }
}