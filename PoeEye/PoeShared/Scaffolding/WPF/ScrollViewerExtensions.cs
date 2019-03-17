using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Common.Logging;
using MahApps.Metro.Controls;

namespace PoeShared.Scaffolding.WPF
{
    public class ScrollViewerExtensions
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ScrollViewerExtensions));

        public static readonly DependencyProperty AutoScrollToTopProperty =
            DependencyProperty.RegisterAttached(
                "AutoScrollToTop",
                typeof(bool),
                typeof(ScrollViewerExtensions),
                new FrameworkPropertyMetadata(false, AutoScrollToTopPropertyChanged));

        public static bool GetAutoScrollToTop(DependencyObject obj)
        {
            return (bool)obj.GetValue(AutoScrollToTopProperty);
        }

        public static void SetAutoScrollToTop(DependencyObject obj, bool value)
        {
            obj.SetValue(AutoScrollToTopProperty, value);
        }

        private static void AutoScrollToTopPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(sender is DependencyObject dependencyObject))
            {
                throw new InvalidOperationException($"Provider object is not DependencyObject: {sender}");
            }
            
            if (!(e.NewValue is bool))
            {
                throw new InvalidOperationException($"Provider value for property {nameof(AutoScrollToTopProperty)} expected to be of type boolean, got {e.NewValue}");
            }
            
            if (!(bool)e.NewValue)
            {
                return;
            }

            var scrollViewer = sender as ScrollViewer ?? dependencyObject.FindChild<ScrollViewer>(string.Empty);
            if (scrollViewer == null)
            {
                var childs = dependencyObject.GetChildObjects().ToArray();
                throw new InvalidOperationException($"Failed to find ScrollViewer for property {nameof(AutoScrollToTopProperty)}, root object: {sender}, childs:\n\t{childs.DumpToTable()}");
            }

            if (Log.IsTraceEnabled)
            {
                Log.Trace($"Scrolling {scrollViewer} to top...");
            }
            scrollViewer.ScrollToTop();
            if (Log.IsTraceEnabled)
            {
                Log.Trace($"Resetting value of {nameof(AutoScrollToTopProperty)} of {scrollViewer} to default value...");
            }
            dependencyObject.SetCurrentValue(AutoScrollToTopProperty, false);
        }
    }
}