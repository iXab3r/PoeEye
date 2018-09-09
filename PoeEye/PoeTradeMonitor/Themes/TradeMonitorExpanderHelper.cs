using System.Windows;
using System.Windows.Controls;

namespace PoeEye.TradeMonitor.Themes
{
    internal sealed class TradeMonitorExpanderHelper
    {
        public static readonly DependencyProperty ExpanderDirectionProperty = DependencyProperty.RegisterAttached(
            "ExpanderDirection", typeof(ExpandDirection), typeof(TradeMonitorExpanderHelper),
            new FrameworkPropertyMetadata(ExpandDirection.Down, FrameworkPropertyMetadataOptions.Inherits));

        public static readonly DependencyProperty ExpandOnHoverProperty = DependencyProperty.RegisterAttached(
            "ExpandOnHover", typeof(bool), typeof(TradeMonitorExpanderHelper), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

        public static void SetExpanderDirection(DependencyObject element, ExpandDirection value)
        {
            element.SetValue(ExpanderDirectionProperty, value);
        }

        public static ExpandDirection GetExpanderDirection(DependencyObject element)
        {
            return (ExpandDirection)element.GetValue(ExpanderDirectionProperty);
        }

        public static void SetExpandOnHover(DependencyObject element, bool value)
        {
            element.SetValue(ExpandOnHoverProperty, value);
        }

        public static bool GetExpandOnHover(DependencyObject element)
        {
            return (bool)element.GetValue(ExpandOnHoverProperty);
        }
    }
}