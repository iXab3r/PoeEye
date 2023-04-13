using System.Windows;
using System.Windows.Controls;

namespace PoeShared.UI;

public static class TabAssist
{
    public static readonly DependencyProperty HasFilledTabProperty = DependencyProperty.RegisterAttached(
        "HasFilledTab", typeof(bool), typeof(TabAssist), new PropertyMetadata(false));

    public static void SetHasFilledTab(DependencyObject element, bool value) => element.SetValue(HasFilledTabProperty, value);

    public static bool GetHasFilledTab(DependencyObject element) => (bool) element.GetValue(HasFilledTabProperty);

    public static readonly DependencyProperty HasUniformTabWidthProperty = DependencyProperty.RegisterAttached(
        "HasUniformTabWidth", typeof(bool), typeof(TabAssist), new PropertyMetadata(false));

    public static void SetHasUniformTabWidth(DependencyObject element, bool value) => element.SetValue(HasUniformTabWidthProperty, value);

    internal static Visibility GetBindableIsItemsHost(DependencyObject obj) => (Visibility)obj.GetValue(BindableIsItemsHostProperty);
    public static bool GetHasUniformTabWidth(DependencyObject element) => (bool) element.GetValue(HasUniformTabWidthProperty);

    internal static void SetBindableIsItemsHost(DependencyObject obj, Visibility value)
        => obj.SetValue(BindableIsItemsHostProperty, value);

    internal static readonly DependencyProperty BindableIsItemsHostProperty =
        DependencyProperty.RegisterAttached("BindableIsItemsHost", typeof(Visibility), typeof(TabAssist), new PropertyMetadata(Visibility.Collapsed, OnBindableIsItemsHostChanged));

    public static readonly DependencyProperty HeaderSuffixContentProperty = DependencyProperty.RegisterAttached(
        "HeaderSuffixContent", typeof(object), typeof(TabAssist), new PropertyMetadata(default(object)));

    public static void SetHeaderSuffixContent(DependencyObject element, object value)
    {
        element.SetValue(HeaderSuffixContentProperty, value);
    }

    public static object GetHeaderSuffixContent(DependencyObject element)
    {
        return (object) element.GetValue(HeaderSuffixContentProperty);
    }

    private static void OnBindableIsItemsHostChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Panel panel)
        {
            panel.IsItemsHost = (Visibility)e.NewValue == Visibility.Visible;
        }
    }
}