using System.Windows.Input;
using PoeShared.Native;
using PoeShared.Scaffolding;

namespace PoeShared.UI.Hotkeys
{
    internal sealed class RecordingNotificationViewModel : NotificationViewModelBase
    {
        private readonly HotkeySequenceEditorViewModel owner;

        public RecordingNotificationViewModel(HotkeySequenceEditorViewModel owner)
        {
            this.owner = owner;
            this.RaiseWhenSourceValue(x => x.StopRecordingHotkey, owner, x => x.StopRecordingHotkey).AddTo(Anchors);
        }

        public HotkeyGesture StopRecordingHotkey => owner.StopRecordingHotkey;

        public ICommand StopRecordingCommand => owner.StopRecording;
    }
}