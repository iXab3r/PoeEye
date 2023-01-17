using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;
using MahApps.Metro.Controls;
using PoeShared.Logging;

namespace PoeShared.Scaffolding.WPF;

public class ScrollViewerExtensions
{
    private static readonly IFluentLog Log = typeof(ScrollViewerExtensions).PrepareLogger();

    public static readonly DependencyProperty AutoScrollProperty =
        DependencyProperty.RegisterAttached("AutoScroll", typeof(bool), typeof(ScrollViewerExtensions), new PropertyMetadata(false, AutoScrollPropertyChanged));

    private static void AutoScrollPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var scrollViewer = sender as ScrollViewer ?? sender.FindChild<ScrollViewer>(string.Empty);
        if ((bool) args.NewValue)
        {
            scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;
            scrollViewer.ScrollToEnd();
        }
        else
        {
            scrollViewer.ScrollChanged -= ScrollViewer_ScrollChanged;
        }
    }

    private static void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        // Only scroll to bottom when the extent changed. Otherwise you can't scroll up
        if (e.ExtentHeightChange != 0)
        {
            var scrollViewer = sender as ScrollViewer;
            scrollViewer?.ScrollToBottom();
        }
    }

    public static bool GetAutoScroll(DependencyObject obj)
    {
        return (bool) obj.GetValue(AutoScrollProperty);
    }

    public static void SetAutoScroll(DependencyObject obj, bool value)
    {
        obj.SetValue(AutoScrollProperty, value);
    }

    public static readonly DependencyProperty AutoScrollToTopProperty =
        DependencyProperty.RegisterAttached(
            "AutoScrollToTop",
            typeof(bool),
            typeof(ScrollViewerExtensions),
            new FrameworkPropertyMetadata(false, AutoScrollToTopPropertyChanged));

    public static bool GetAutoScrollToTop(DependencyObject obj)
    {
        return (bool) obj.GetValue(AutoScrollToTopProperty);
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

        if (!(bool) e.NewValue)
        {
            return;
        }

        var scrollViewer = sender as ScrollViewer ?? dependencyObject.FindChild<ScrollViewer>(string.Empty);
        if (scrollViewer == null)
        {
            var childs = dependencyObject.GetChildObjects().ToArray();
            throw new InvalidOperationException($"Failed to find ScrollViewer for property {nameof(AutoScrollToTopProperty)}, root object: {sender}, childs:\n\t{childs.DumpToString()}");
        }

        if (Log.IsDebugEnabled)
        {
            Log.Debug(() => $"Scrolling {scrollViewer} to top...");
        }

        scrollViewer.ScrollToTop();
        if (Log.IsDebugEnabled)
        {
            Log.Debug(() => $"Resetting value of {nameof(AutoScrollToTopProperty)} of {scrollViewer} to default value...");
        }

        dependencyObject.SetCurrentValue(AutoScrollToTopProperty, false);
    }
}