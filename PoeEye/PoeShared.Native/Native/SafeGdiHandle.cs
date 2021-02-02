using System;
using System.Security;
using Microsoft.Win32.SafeHandles;
using PInvoke;

namespace PoeShared.Native.Native
{
    public class SafeGdiHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        [SecurityCritical]
        public SafeGdiHandle(IntPtr preexistingHandle)
            : base(true)
        {
            SetHandle(preexistingHandle);
        }

        protected override bool ReleaseHandle()
        {
            return Gdi32.DeleteObject(handle);
        }
    }
}