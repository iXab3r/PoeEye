using PoeShared.Native;

namespace PoeShared.UI
{
    internal sealed class RecordingNotificationViewModel : NotificationViewModelBase
    {
        public RecordingNotificationViewModel(IHotkeySequenceEditorViewModel owner)
        {
            Owner = owner;
        }

        public IHotkeySequenceEditorViewModel Owner { get; }
    }
}