using System.Windows;

namespace PoeShared.UI;

public class TreeViewAssist
{
    public static readonly DependencyProperty IsExpandableProperty = DependencyProperty.RegisterAttached(
        "IsExpandable", typeof(bool), typeof(TreeViewAssist), new PropertyMetadata(true));

    public static void SetIsExpandable(DependencyObject element, bool value)
    {
        element.SetValue(IsExpandableProperty, value);
    }

    public static bool GetIsExpandable(DependencyObject element)
    {
        return (bool)element.GetValue(IsExpandableProperty);
    }
}