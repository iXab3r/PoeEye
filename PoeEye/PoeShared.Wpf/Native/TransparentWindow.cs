using System;
using System.Windows;
using PInvoke;
using PoeShared.Scaffolding;

namespace PoeShared.Native
{
    public class TransparentWindow : ConstantAspectRatioWindow
    {
        private const int HWND_MESSAGE = -3;
        private IntPtr initialParent;

        public TransparentWindow()
        {
            Loaded += OnLoaded;
            SourceInitialized += OnSourceInitialized;
            Log.Debug(() => "Created window");
        }

        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            Log.Debug(() => $"Making window message-only");
            initialParent = User32.SetParent(WindowHandle, (IntPtr)HWND_MESSAGE);
            Log.Debug(() => $"Set window parent to {HWND_MESSAGE.ToHexadecimal()}, was: {initialParent.ToHexadecimal()}");
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Log.Debug(() => "Window is loaded - hiding system menu");
            UnsafeNative.HideSystemMenu(WindowHandle);
            Log.Debug(() => $"Setting window parent to {initialParent.ToHexadecimal()}");
            var previousParent = User32.SetParent(WindowHandle, default);
        }

        protected void MakeTransparent()
        {
            Log.Debug(() => "Making window transparent");
            UnsafeNative.SetWindowExTransparent(WindowHandle);
        }

        protected void MakeLayered()
        {
            Log.Debug(() => "Making window layered");
            UnsafeNative.SetWindowExLayered(WindowHandle);
        }
    }
}