using System;
using System.Drawing;

namespace PoeShared.Native
{
    [Obsolete("Will be removed in 2020, use UnsafeNative")]
    public static class WindowsServices
    {
        static WindowsServices()
        {
            UnsafeNative.AllowSetForegroundWindow();
        }

        public static bool SetWindowRect(IntPtr hwnd, Rectangle rect)
        {
            return UnsafeNative.SetWindowRect(hwnd, rect);
        }

        public static void HideSystemMenu(IntPtr hwnd)
        {
            UnsafeNative.HideSystemMenu(hwnd);
        }

        public static void ShowInactiveTopmost(IntPtr handle, int left, int top, int width, int height)
        {
            UnsafeNative.ShowInactiveTopmost(handle, left, top, width, height);
        }

        public static void ShowWindow(IntPtr handle)
        {
            UnsafeNative.ShowWindow(handle);
        }

        public static void HideWindow(IntPtr handle)
        {
            UnsafeNative.HideWindow(handle);
        }

        public static void SetWindowExTransparent(IntPtr hwnd)
        {
            UnsafeNative.SetWindowExTransparent(hwnd);
        }

        public static void SetWindowExLayered(IntPtr hwnd)
        {
            UnsafeNative.SetWindowExLayered(hwnd);
        }

        public static void SetWindowExNoActivate(IntPtr hwnd)
        {
            UnsafeNative.SetWindowExNoActivate(hwnd);
        }
    }
}