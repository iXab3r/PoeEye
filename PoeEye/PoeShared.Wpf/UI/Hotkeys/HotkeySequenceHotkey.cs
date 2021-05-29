using System;
using System.Drawing;
using System.Reactive.Linq;
using System.Windows.Input;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.UI.Hotkeys
{
    public sealed class HotkeySequenceHotkey : HotkeySequenceItem
    {
        private readonly ObservableAsPropertyHelper<bool> isMainMouseButton;
        private readonly ObservableAsPropertyHelper<bool> hasMousePosition;

        private HotkeyGesture hotkey;
        private Point? mousePosition;
        private bool? isDown;

        public HotkeySequenceHotkey()
        {
            this.WhenAnyValue(x => x.MousePosition)
                .Select(x => x != null && !x.Value.IsEmpty)
                .ToProperty(out hasMousePosition, this, x => x.HasMousePosition)
                .AddTo(Anchors);
            
            this.WhenAnyValue(x => x.Hotkey, x => x.HasMousePosition)
                .Select(x => (Hotkey?.MouseButton is MouseButton.Left or MouseButton.Right or MouseButton.Middle) || HasMousePosition)
                .ToProperty(out isMainMouseButton, this, x => x.IsMouse)
                .AddTo(Anchors);
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

        public bool IsMouse => isMainMouseButton.Value;

        public bool HasMousePosition => hasMousePosition.Value;

        public bool? IsDown
        {
            get => isDown;
            set => RaiseAndSetIfChanged(ref isDown, value);
        }
    }
}