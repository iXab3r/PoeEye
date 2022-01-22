using System.Windows.Forms;
using EnumsNET;
using PoeShared.UI;

namespace PoeShared.Scaffolding;

public static class HotkeyGestureExtensions
{
    public static bool Contains(this HotkeyGesture gesture, Keys keys)
    {
        var modifiers = keys.ToModifiers();
        var key = keys.ToInputKey();

        return gesture.Key == key || gesture.ModifierKeys.HasAnyFlags(modifiers);
    }
}