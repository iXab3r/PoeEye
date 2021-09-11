using System.Globalization;
using System.Windows.Forms;
using PoeShared.Native;

namespace PoeShared.Services
{
    public sealed record KeyboardLayout
    {
        internal KeyboardLayout(InputLanguage inputLanguage) : this((uint)inputLanguage.Handle, inputLanguage.LayoutName)
        {
        }
        
        public KeyboardLayout(uint hkl, string layoutName)
        {
            LayoutId = hkl;
            LayoutName = layoutName;
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