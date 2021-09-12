using System;
using System.Globalization;
using System.Windows.Forms;
using PoeShared.Native;

namespace PoeShared.Services
{
    public sealed record KeyboardLayout
    {
        public KeyboardLayout(InputLanguage inputLanguage)
        {
            InputLanguage = inputLanguage;
            Handle = inputLanguage.Handle;
            LayoutName = inputLanguage.LayoutName;
            LCID = (ushort)((uint)Handle >> 16);
            PrimaryLanguageId = (byte)(LCID << 8 >> 8);
            SubLanguageId = (byte)(LCID >> 8);
            Culture = CultureInfo.GetCultureInfo(LCID);
        }

        public string LayoutName { get; }
        
        public ushort LCID { get; }
        
        public byte PrimaryLanguageId { get; }
        
        public byte SubLanguageId { get; }

        public IntPtr Handle { get; }
        
        public InputLanguage InputLanguage { get; }
        
        public CultureInfo Culture { get; }

        public bool IsValid => Handle != IntPtr.Zero && !string.IsNullOrEmpty(LayoutName);
    }
}