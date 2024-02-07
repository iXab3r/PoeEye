using System.Windows;
using System.Windows.Input;
using Microsoft.Web.WebView2.Wpf;

namespace PoeShared.Blazor.Wpf;

/// <summary>
/// Problem statement:
/// If WebView2 is hosted inside ScrollViewer or ListBox, some key combinations won't work, e.g. PageUp/PageDown or Control+A (with ListBox).
/// 
/// Why?
/// This is due to how input system works in WebView2 - it tries to simulate WPF events pipeline (with tunneling/bubbling) and relies on the fact that event should not be handled by anyone in the chain for the input to be processed by the browser.
/// 
/// From the docs of WebView2:
/// 
///   /// Accelerator key presses (e.g. Ctrl+P) that occur within the control will
///   /// fire standard key press events such as OnKeyDown. You can suppress the
///   /// control's default implementation of an accelerator key press (e.g.
///   /// printing, in the case of Ctrl+P) by setting the Handled property of its
///   /// EventArgs to true. Also note that the underlying browser process is
///   /// blocked while these handlers execute, so:
/// Pipeline works like this:
/// 
/// WebView2 is hosted in a separate window, so all things around how it handles the input have to be handled in non-straighforward manner ("native" to any framework)
/// 
/// When WebView2 is instantiated, subscription is done to AcceleratorKeyPressed (which calls ICoreWebView2 add_AcceleratorKeyPressed internally). This is how WebView2 input system is wired with WPF world.
/// 
/// 2') There is a flag, called _browserHitTransparent, which will omit that subscription altogether, "fixing" the bug, but you will lose all control over keys pressed in the browser
/// 
/// After subscription is done, any key press, considered "accelerator", will be propagated through WPF event system. If nothing marked even as Handled, accelerator will do its job on browser side
/// refer to this section for more details
/// 
/// So, to get the key working on browser side, you have to make sure that NOTHING handles the same key on WPF side in the whole huge bubbling chain. If something in WPF app has marked event as Handled - it will notify browser-side, that someone "consumed" that keypress, which will stop processing on browser-side.
/// 
/// Unfortunately, this does not work very well with RoutedUICommands and bubbling in WPF.
/// 
/// For example, both ScrollViewer(e.g. PageUp/Down) and ListBox(Ctrl+A) are registering a bunch of input gestures and intercept keypresses (marking them as Handled whenever they catch them and they can process them).
/// If you have WebView hosted in either of those, any registered input gesture that is also considered "accelerator" will take precedence over anything happening inside browser. In my case text editing was basically impossible as all the usual combinations were consumed by either ScrollViewer or by ListBox
/// 
/// At this point you have two options:
/// 
/// Make it so NOTHING will intercept KeyDown event all the way to the root. Doable, but could be very hard - having 0 controls doing input gestures is quite unusual in WPF apps.
/// 
/// Somehow prevent the bubbling system from working. E.g. suppressing it at the very beginning. That is my route.
/// </summary>
internal sealed class WebView2Ex : WebView2
{
    public static readonly DependencyProperty IgnoreKeypressesProperty = DependencyProperty.Register(
        nameof(IgnoreKeypresses), typeof(bool), typeof(WebView2Ex), new PropertyMetadata(defaultValue: true));

    /// <summary>
    /// This options makes WebView2 ignore built-in WPF input simulation mechanism,
    /// see WebView2.OnKeyDown comment for more details.
    /// The problem with existing approach is that bubbling events when WebView2 is hosted
    /// in ScrollViewer or ListBox will lead to THOSE controls capturing inputs, which will in turn
    /// disabled WebView2 input handling mechanism.
    /// E.g. scrolling will happen in WPF ScrollViewer when user will be navigating on the page using PageUp/PageDown
    /// </summary>
    public bool IgnoreKeypresses
    {
        get => (bool) GetValue(IgnoreKeypressesProperty);
        set => SetValue(IgnoreKeypressesProperty, value);
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (IgnoreKeypresses)
        {
            return;
        } 
        base.OnPreviewKeyDown(e);
    }

    protected override void OnPreviewKeyUp(KeyEventArgs e)
    {
        if (IgnoreKeypresses)
        {
            return;
        } 
        base.OnPreviewKeyUp(e);
    }
}