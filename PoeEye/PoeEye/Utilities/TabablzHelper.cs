using System.Windows;
using Dragablz;

namespace PoeEye.Utilities
{
    internal sealed class TabablzHelper
    {
        public static readonly DependencyProperty PositionMonitorProperty = DependencyProperty.RegisterAttached(
            "PositionMonitor", typeof(PositionMonitor), typeof(TabablzHelper), new FrameworkPropertyMetadata(default(PositionMonitor), FrameworkPropertyMetadataOptions.Inherits));

        public static void SetPositionMonitor(DependencyObject element, PositionMonitor value)
        {
            element.SetValue(PositionMonitorProperty, value);
        }

        public static PositionMonitor GetPositionMonitor(DependencyObject element)
        {
            return (PositionMonitor) element.GetValue(PositionMonitorProperty);
        }
    }
}