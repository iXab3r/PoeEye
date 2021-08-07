using System.Runtime.InteropServices;

namespace WindowsHook.Implementation
{
    internal static class NativeMethods
    {
        private const int SM_CXDRAG = 68;
        private const int SM_CYDRAG = 69;

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int index);

        public static int GetXDragThreshold()
        {
            return GetSystemMetrics(SM_CXDRAG);
        }

        public static int GetYDragThreshold()
        {
            return GetSystemMetrics(SM_CYDRAG);
        }
    }
}