using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace PoeShared.Scaffolding.WPF;

public static class MouseWheelHelper
{
    public static readonly DependencyProperty IgnoreMouseWheelProperty = DependencyProperty.RegisterAttached(
        "IgnoreMouseWheel", typeof(bool), typeof(MouseWheelHelper), 
        new FrameworkPropertyMetadata(false, IgnoreMouseWheelPropertyChanged));
        
    public static void SetIgnoreMouseWheel(DependencyObject element, bool value)
    {
        element.SetValue(IgnoreMouseWheelProperty, value);
    }

    public static bool GetIgnoreMouseWheel(DependencyObject element)
    {
        return (bool) element.GetValue(IgnoreMouseWheelProperty);
    }
        
    public static void HandlePreviewMouseWheel(
        DependencyObject sender, 
        MouseWheelEventArgs e, 
        bool ignoreWheel)
    {
        var parent = VisualTreeHelper.GetParent(sender);
        if (!(parent is UIElement))
        {
            return;
        }

        ((UIElement) parent).RaiseEvent(
            new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta) { RoutedEvent = UIElement.MouseWheelEvent });

        if (ignoreWheel)
        {
            e.Handled = true;
        }
    }
        
    private static void IgnoreMouseWheelPropertyChanged(DependencyObject associatedObject, DependencyPropertyChangedEventArgs valueToSet)
    {
        var element = associatedObject as UIElement;
        if (element == null)
        {
            return;
        }
            
        bool? newValue = null;
        if (valueToSet.NewValue is string)
        {
            if (bool.TryParse(valueToSet.NewValue as string, out var newBool))
            {
                newValue = newBool;
            }
        }

        if (valueToSet.NewValue is bool)
        {
            newValue = (bool) valueToSet.NewValue;
        }

        if (newValue == null)
        {
            return;
        }
            
        if (!newValue.Value)
        {
            element.PreviewMouseWheel -= ElementOnPreviewMouseWheel;
        }
        else
        {
            element.PreviewMouseWheel += ElementOnPreviewMouseWheel;
        }
    }
        
    private static void ElementOnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!(sender is DependencyObject dependencyObject))
        {
            return;
        }
            
        HandlePreviewMouseWheel(dependencyObject, e, true);
    }
}