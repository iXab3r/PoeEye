namespace PoeShared.UI
{
    public sealed class HotkeySequenceText : HotkeySequenceItem
    {
        private string text;

        public string Text
        {
            get => text;
            set => RaiseAndSetIfChanged(ref text, value);
        }
    }
}