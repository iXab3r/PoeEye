using System.Windows;

namespace PoeShared.Scaffolding.WPF
{
    public static class ContextMenuServiceExtensions
    {
        public static readonly DependencyProperty DataContextProperty =
            DependencyProperty.RegisterAttached(
                "DataContext",
                typeof(object),
                typeof(ContextMenuServiceExtensions),
                new UIPropertyMetadata(DataContextChanged));

        public static readonly DependencyProperty BindDataContextToMenuItemCommandParameterProperty = DependencyProperty.RegisterAttached(
            "BindDataContextToMenuItemCommandParameter", typeof(bool), 
            typeof(ContextMenuServiceExtensions), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

        public static void SetBindDataContextToMenuItemCommandParameter(DependencyObject element, bool value)
        {
            element.SetValue(BindDataContextToMenuItemCommandParameterProperty, value);
        }

        public static bool GetBindDataContextToMenuItemCommandParameter(DependencyObject element)
        {
            return (bool) element.GetValue(BindDataContextToMenuItemCommandParameterProperty);
        }

        public static object GetDataContext(FrameworkElement obj)
        {
            return obj.GetValue(DataContextProperty);
        }

        public static void SetDataContext(FrameworkElement obj, object value)
        {
            obj.SetValue(DataContextProperty, value);
        }

        private static void DataContextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var frameworkElement = d as FrameworkElement;

            if (frameworkElement?.ContextMenu != null)
            {
                frameworkElement.ContextMenu.DataContext = GetDataContext(frameworkElement);
            }
        }
    }
}