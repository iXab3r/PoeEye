using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using PInvoke;
using PoeShared.Prism;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PoeShared.Services;

namespace PoeShared.UI;

internal sealed class CharToKeysConverter : IConverter<char, Keys>, IConverter<(char ch, KeyboardLayout layout), Keys>
{
    private static readonly IFluentLog Log = typeof(CharToKeysConverter).PrepareLogger();

    public Keys Convert((char ch, KeyboardLayout layout) value)
    {
        return Convert(value.ch, value.layout);
    }

    public Keys Convert(char value)
    {
        var vkey = VkKeyScan(value);
        return ConvertScanToKeys(vkey);
    }

    private Keys Convert(char ch, KeyboardLayout layout)
    {
        if (layout == default)
        {
            return Convert(ch);
        }

        var scanCode = VkKeyScanEx(ch, layout.Handle);
        return ConvertScanToKeys(scanCode);
    }

    private static Keys ConvertScanToKeys(short scanCode) {
        Keys retval = (Keys)(scanCode & 0xff);  
        int modifiers = scanCode >> 8;

        if ((modifiers & 0x38) != 0)
        {
            // "The Hankaku key is pressed" or either of the "Reserved" state bits (for instance, used by Neo2 keyboard layout).
            // Callers expect failure in this case so that a fallback method can be used.
            return 0;
        }

        if ((modifiers & 1) != 0) retval |= Keys.Shift;
        if ((modifiers & 2) != 0) retval |= Keys.Control;
        if ((modifiers & 4) != 0) retval |= Keys.Alt;

        return retval;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern short VkKeyScan(char ch);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern short VkKeyScanEx(char ch, IntPtr hkl);
}