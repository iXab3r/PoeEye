using System.Drawing;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PropertyBinder;

namespace PoeShared.UI
{
    public sealed class HotkeySequenceHotkey : HotkeySequenceItem
    {
        private static readonly Binder<HotkeySequenceHotkey> Binder = new();

        static HotkeySequenceHotkey()
        {
            Binder.Bind(x => x.MousePosition != null).To(x => x.HasMousePosition);
            Binder.Bind(x => x.HasMousePosition || x.Hotkey != null && x.Hotkey.MouseButton != null).To(x => x.IsMouse);
        }

        public HotkeySequenceHotkey()
        {
            Binder.Attach(this).AddTo(Anchors);
        }

        public HotkeyGesture Hotkey { get; set; }

        public bool IsRelative { get; set; }

        public Point? MousePosition { get; set; }

        public bool HasMousePosition { get; private set; }

        public bool IsMouse { get; private set; }

        public bool? IsDown { get; set; }
    }
}