using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Dragablz;

namespace PoeEye.Utilities
{
    internal sealed class TabablzHelper
    {
        public static readonly DependencyProperty PositionMonitorProperty = DependencyProperty.RegisterAttached(
            "PositionMonitor", typeof(PositionMonitor), typeof(TabablzHelper),
            new FrameworkPropertyMetadata(default(PositionMonitor), FrameworkPropertyMetadataOptions.Inherits));

        public static readonly DependencyProperty ItemContextMenuProperty = DependencyProperty.RegisterAttached(
            "ItemContextMenu", typeof(ContextMenu), typeof(TabablzHelper),
            new FrameworkPropertyMetadata(default(ContextMenu), FrameworkPropertyMetadataOptions.Inherits));

        public static readonly DependencyProperty PreviewMouseRightButtonDownHandlerProperty = DependencyProperty.RegisterAttached(
            "PreviewMouseRightButtonDownHandler", typeof(ICommand), typeof(TabablzHelper),
            new FrameworkPropertyMetadata(default(ICommand), FrameworkPropertyMetadataOptions.Inherits));

        public static void SetPositionMonitor(DependencyObject element, PositionMonitor value)
        {
            element.SetValue(PositionMonitorProperty, value);
        }

        public static void SetPreviewMouseRightButtonDownHandler(DependencyObject element, ICommand value)
        {
            element.SetValue(PreviewMouseRightButtonDownHandlerProperty, value);
        }

        public static ICommand GetPreviewMouseRightButtonDownHandler(DependencyObject element)
        {
            return (ICommand)element.GetValue(PreviewMouseRightButtonDownHandlerProperty);
        }

        public static PositionMonitor GetPositionMonitor(DependencyObject element)
        {
            return (PositionMonitor)element.GetValue(PositionMonitorProperty);
        }

        public static void SetItemContextMenu(DependencyObject element, ContextMenu value)
        {
            element.SetValue(ItemContextMenuProperty, value);
        }

        public static ContextMenu GetItemContextMenu(DependencyObject element)
        {
            return (ContextMenu)element.GetValue(ItemContextMenuProperty);
        }
    }
}