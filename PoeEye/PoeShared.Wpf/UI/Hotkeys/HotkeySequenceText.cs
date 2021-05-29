namespace PoeShared.UI.Hotkeys
{
    public sealed class HotkeySequenceText : HotkeySequenceItem
    {
        private string text;

        public HotkeySequenceText()
        {
        }

        public string Text
        {
            get => text;
            set => RaiseAndSetIfChanged(ref text, value);
        }
    }
}