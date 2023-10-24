using System.Drawing;
using JetBrains.Annotations;
using PoeShared.Scaffolding;
using PropertyBinder;

namespace PoeShared.UI;

public sealed class HotkeySequenceHotkey : HotkeySequenceItem
{
    private static readonly Binder<HotkeySequenceHotkey> Binder = new();

    static HotkeySequenceHotkey()
    {
        Binder.Bind(x => x.MousePosition != null).To(x => x.HasMousePosition);
        Binder.Bind(x => x.HasMousePosition || x.Hotkey != null && x.Hotkey.IsMouse).To(x => x.IsMouse);
    }

    public HotkeySequenceHotkey()
    {
        Binder.Attach(this).AddTo(Anchors);
    }

    public HotkeyGesture Hotkey { get; set; }

    public bool IsRelative { get; set; }

    public Point? MousePosition { get; set; }

    public bool HasMousePosition { get; [UsedImplicitly] private set; }

    public bool IsMouse { get; [UsedImplicitly] private set; }

    public bool? IsDown { get; set; }
}