using System.Windows;
using System.Windows.Controls;

namespace PoeShared.Scaffolding.WPF;

public static class WebBrowserHelper
{
    public static readonly DependencyProperty HtmlProperty = DependencyProperty.RegisterAttached(
        "Html",
        typeof(string),
        typeof(WebBrowserHelper),
        new FrameworkPropertyMetadata(OnHtmlChanged));

    [AttachedPropertyBrowsableForType(typeof(WebBrowser))]
    public static string GetHtml(WebBrowser d)
    {
        return (string) d.GetValue(HtmlProperty);
    }

    public static void SetHtml(WebBrowser d, string value)
    {
        d.SetValue(HtmlProperty, value);
    }

    private static void OnHtmlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WebBrowser wb)
        {
            wb.NavigateToString(e.NewValue as string ?? string.Empty);
        }
    }
}