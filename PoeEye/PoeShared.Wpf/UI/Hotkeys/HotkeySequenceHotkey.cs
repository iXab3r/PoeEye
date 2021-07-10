using System.Drawing;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PropertyBinder;

namespace PoeShared.UI
{
    public sealed class HotkeySequenceHotkey : HotkeySequenceItem
    {
        private static readonly Binder<HotkeySequenceHotkey> Binder = new();
        private bool hasMousePosition;
        private bool isMouse;
        private bool? isDown;
        private Point? mousePosition;
        private HotkeyGesture hotkey;

        static HotkeySequenceHotkey()
        {
            Binder.Bind(x => x.MousePosition != null).To(x => x.HasMousePosition);
            Binder.Bind(x => x.HasMousePosition || x.Hotkey != null && x.Hotkey.MouseButton != null).To(x => x.IsMouse);
        }

        public HotkeySequenceHotkey()
        {
            Binder.Attach(this).AddTo(Anchors);
        }

        public HotkeyGesture Hotkey
        {
            get => hotkey;
            set => RaiseAndSetIfChanged(ref hotkey, value);
        }

        public Point? MousePosition
        {
            get => mousePosition;
            set => RaiseAndSetIfChanged(ref mousePosition, value);
        }

        public bool HasMousePosition
        {
            get => hasMousePosition;
            private set => RaiseAndSetIfChanged(ref hasMousePosition, value);
        }

        public bool IsMouse
        {
            get => isMouse;
            private set => RaiseAndSetIfChanged(ref isMouse, value);
        }

        public bool? IsDown
        {
            get => isDown;
            set => RaiseAndSetIfChanged(ref isDown, value);
        }
    }
}