using System.Windows.Forms;
using PoeShared.Prism;

namespace PoeShared.UI.Hotkeys
{
    internal sealed class CharToKeysConverter : IConverter<char, Keys>
    {
        public Keys Convert(char value)
        {
            return ConvertCharToVirtualKey(value);
        }
        
        public static Keys ConvertCharToVirtualKey(char ch) {
            short vkey = VkKeyScan(ch);
            Keys retval = (Keys)(vkey & 0xff);
            int modifiers = vkey >> 8;

            if ((modifiers & 1) != 0) retval |= Keys.Shift;
            if ((modifiers & 2) != 0) retval |= Keys.Control;
            if ((modifiers & 4) != 0) retval |= Keys.Alt;

            return retval;
        }
        
        
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern short VkKeyScan(char ch);
    }
}