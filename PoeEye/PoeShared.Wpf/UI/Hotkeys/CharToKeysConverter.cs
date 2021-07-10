using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using log4net;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding; 
using PoeShared.Logging;

namespace PoeShared.UI
{
    internal sealed class CharToKeysConverter : IConverter<char, Keys>, IConverter<(char ch, string keyboardLayoutId), Keys>
    {
        private static readonly IFluentLog Log = typeof(CharToKeysConverter).PrepareLogger();
        private readonly ICollection<KeyboardLayout> keyboardHandleByKeyboardLayoutId;
        
        public CharToKeysConverter()
        {
            keyboardHandleByKeyboardLayoutId = UnsafeNative.GetKeyboardLayoutList()
                .Select(x => new KeyboardLayout
                {
                    KeyboardLayoutHandle = x,
                    LayoutName = UnsafeNative.GetKeyboardLayoutName(x),
                })
                .ToReadOnlyObservableCollection();
            
            Log.Debug($"Known layouts: {keyboardHandleByKeyboardLayoutId.DumpToString()}");
        }

        public Keys Convert(char value)
        {
            var vkey = VkKeyScan(value);
            return ConvertScanToKeys(vkey);
        }
        
        public Keys Convert(char ch, string keyboardLayoutId)
        {
            var keyboardLayout = keyboardHandleByKeyboardLayoutId.FirstOrDefault(x => x.LayoutName == keyboardLayoutId);
            if (!keyboardLayout.IsValid)
            {
                return Keys.None;
            }

            var scanCode = VkKeyScanEx(ch, keyboardLayout.KeyboardLayoutHandle);
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

        private struct KeyboardLayout
        {
            public bool IsValid => KeyboardLayoutHandle != IntPtr.Zero;
            
            public string LayoutName { get; set; }
            
            public IntPtr KeyboardLayoutHandle { get; set; }
        }
    }
}