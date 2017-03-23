using System;
using System.Runtime.InteropServices;

namespace PoeWhisperMonitor
{
    internal static class NativeMethods
    {
        /// <summary>
        ///     The GetForegroundWindow function returns a handle to the foreground window.
        /// </summary>
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}