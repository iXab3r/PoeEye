using System.Globalization;
using PoeShared.Native;

namespace PoeShared.Services
{
    public sealed record KeyboardLayout
    {
        public KeyboardLayout(uint hkl)
        {
            LayoutId = hkl;
            LayoutName = UnsafeNative.GetKeyboardLayoutName(hkl);
            LCID = (ushort)(LayoutId >> 16);
            PrimaryLanguageId = (byte)(LCID << 8 >> 8);
            SubLanguageId = (byte)(LCID >> 8);
            Culture = System.Globalization.CultureInfo.GetCultureInfo(LCID);
        }

        public string LayoutName { get; }
        
        public ushort LCID { get; }
        
        public byte PrimaryLanguageId { get; }
        
        public byte SubLanguageId { get; }

        public uint LayoutId { get; }
        
        public CultureInfo Culture { get; }

        public bool IsValid => LayoutId > 0 && !string.IsNullOrEmpty(LayoutName);
    }
}