using System.Windows;

namespace PoeShared.UI;

public class TextBoxAssist
{
    public static readonly DependencyProperty IsExpandableProperty = DependencyProperty.RegisterAttached(
        "IsExpandable", typeof(bool), typeof(TextBoxAssist), new PropertyMetadata(default(bool)));

    public static void SetIsExpandable(DependencyObject element, bool value)
    {
        element.SetValue(IsExpandableProperty, value);
    }

    public static bool GetIsExpandable(DependencyObject element)
    {
        return (bool) element.GetValue(IsExpandableProperty);
    }
}