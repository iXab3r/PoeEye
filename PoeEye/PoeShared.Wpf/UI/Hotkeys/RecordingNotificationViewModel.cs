using PoeShared.Native;

namespace PoeShared.UI.Hotkeys
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