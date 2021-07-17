using PoeShared.Native;
using PoeShared.Notifications;
using PoeShared.Notifications.ViewModels;

namespace PoeShared.UI
{
    internal sealed class RecordingNotificationViewModel : NotificationViewModelBase
    {
        public RecordingNotificationViewModel(IHotkeySequenceEditorViewModel owner)
        {
            Owner = owner;
            Closeable = false;
            Interactive = false;
        }

        public IHotkeySequenceEditorViewModel Owner { get; }
    }
}