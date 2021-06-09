using System;

namespace PoeShared.UI
{
    public sealed class HotkeySequenceDelay : HotkeySequenceItem
    {
        private TimeSpan delay;
        private bool isKeypress;

        public TimeSpan Delay
        {
            get => delay;
            set => RaiseAndSetIfChanged(ref delay, value);
        }

        public bool IsKeypress
        {
            get => isKeypress;
            set => RaiseAndSetIfChanged(ref isKeypress, value);
        }
    }
}