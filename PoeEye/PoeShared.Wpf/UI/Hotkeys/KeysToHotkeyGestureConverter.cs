using System.Windows.Forms;
using PoeShared.Prism;
using PoeShared.Scaffolding; 
using PoeShared.Logging;

namespace PoeShared.UI
{
    internal sealed class KeysToHotkeyGestureConverter : IConverter<Keys, HotkeyGesture>
    {
        public HotkeyGesture Convert(Keys value)
        {
            var wpfKey = value.ToInputKey();
            var modifiers = value.ToModifiers();
            return new HotkeyGesture(wpfKey, modifiers);
        }
    }
}