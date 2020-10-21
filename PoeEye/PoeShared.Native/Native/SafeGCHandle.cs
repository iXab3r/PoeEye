using System;
using System.Runtime.InteropServices;

namespace PoeShared.Native
{
    public sealed class SafeGCHandle : IDisposable
    {
        private GCHandle gcHandle;
        private bool isDisposed;

        public SafeGCHandle(GCHandle gcHandle)
        {
            this.gcHandle = gcHandle;
        }

        public T ToStructure<T>()
        {
            return Marshal.PtrToStructure<T>(gcHandle.AddrOfPinnedObject());
        }
        
        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }
            
            if (gcHandle.IsAllocated)
            {
                gcHandle.Free();
            }

            isDisposed = true;
        }
    }
}