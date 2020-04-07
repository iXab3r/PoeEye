using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Media;

namespace PoeShared.Scaffolding.WPF
{
    public sealed class IgnoreMouseWheelBehavior : Behavior<UIElement>
    {
        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.Register(
            "IsEnabled",
            typeof(bool),
            typeof(IgnoreMouseWheelBehavior),
            new PropertyMetadata(true));

        public bool IsEnabled
        {
            get { return (bool) GetValue(IsEnabledProperty); }
            set { SetValue(IsEnabledProperty, value); }
        }
        
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewMouseWheel += AssociatedObject_PreviewMouseWheel;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PreviewMouseWheel -= AssociatedObject_PreviewMouseWheel;
            base.OnDetaching();
        }

        private void AssociatedObject_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!(sender is DependencyObject dependencyObject))
            {
                return;
            }

            DependencyObject parent = VisualTreeHelper.GetParent((DependencyObject) sender);
            if (!(parent is UIElement))
            {
                return;
            }

            ((UIElement) parent).RaiseEvent(
                new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta) { RoutedEvent = UIElement.MouseWheelEvent });

            if (IsEnabled)
            {
                e.Handled = true;
            }
        }
    }
}