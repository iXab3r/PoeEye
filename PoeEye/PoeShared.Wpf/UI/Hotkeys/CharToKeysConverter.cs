using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Windows.Forms;
using log4net;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;

namespace PoeShared.UI.Hotkeys
{
    internal sealed class CharToKeysConverter : IConverter<char, Keys>, IConverter<(char ch, string keyboardLayoutId), Keys>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(CharToKeysConverter));
        private readonly ConcurrentDictionary<string, IntPtr> keyboardHandleByKeyboardLayoutId;
        
        public CharToKeysConverter()
        {
            keyboardHandleByKeyboardLayoutId = 
                new ConcurrentDictionary<string, IntPtr>(
                    UnsafeNative.GetKeyboardLayoutList()
                        .Select(x => (x, UnsafeNative.GetKeyboardLayoutName(x)))
                        .ToDictionary(x => x.Item2, x => x.x));
            
            Log.Debug($"Known layouts: {keyboardHandleByKeyboardLayoutId.DumpToString()}");
        }

        public Keys Convert(char value)
        {
            var vkey = VkKeyScan(value);
            return ConvertScanToKeys(vkey);
        }
        
        public Keys Convert(char ch, string keyboardLayoutId)
        {
            if (!keyboardHandleByKeyboardLayoutId.TryGetValue(keyboardLayoutId, out var hkl))
            {
                return Keys.None;
            }

            var scanCode = VkKeyScanEx(ch, hkl);
            return ConvertScanToKeys(scanCode);
        }
        
        private static Keys ConvertScanToKeys(short scanCode) {
            Keys retval = (Keys)(scanCode & 0xff);  
            int modifiers = scanCode >> 8;

            if ((modifiers & 1) != 0) retval |= Keys.Shift;
            if ((modifiers & 2) != 0) retval |= Keys.Control;
            if ((modifiers & 4) != 0) retval |= Keys.Alt;

            return retval;
        }
        
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern short VkKeyScan(char ch);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern short VkKeyScanEx(char ch, IntPtr hkl);
        
        public Keys Convert((char ch, string keyboardLayoutId) value)
        {
            return Convert(value.ch, value.keyboardLayoutId);
        }
    }
}