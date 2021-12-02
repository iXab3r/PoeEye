using System.Windows;

namespace PoeShared.Native
{
    public class TransparentWindow : ConstantAspectRatioWindow
    {
        public TransparentWindow()
        {
            Loaded += OnLoaded;
            Log.Debug(() => "Created window");
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Log.Debug(() => "Window is loaded - hiding system menu");
            UnsafeNative.HideSystemMenu(WindowHandle);
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