using System;
using System.Reactive.Linq;
using System.Windows.Input;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.UI.Hotkeys
{
    public sealed class HotkeySequenceHotkey : HotkeySequenceItem
    {
        private bool? isDown;
        private readonly ObservableAsPropertyHelper<bool> isMainMouseButton;
        private HotkeyGesture hotkey;

        public HotkeySequenceHotkey()
        {
            this.WhenAnyValue(x => x.Hotkey)
                .Select(x => x?.MouseButton is MouseButton.Left or MouseButton.Right or MouseButton.Middle)
                .ToProperty(out isMainMouseButton, this, x => x.IsMainMouseButton)
                .AddTo(Anchors);
        }

        public HotkeyGesture Hotkey
        {
            get => hotkey;
            set => RaiseAndSetIfChanged(ref hotkey, value);
        }

        public bool IsMainMouseButton => isMainMouseButton.Value;

        public bool? IsDown
        {
            get => isDown;
            set => RaiseAndSetIfChanged(ref isDown, value);
        }
    }
}