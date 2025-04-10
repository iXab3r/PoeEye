using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Wpf.Scaffolding;

internal static class FrameworkElementExtensions
{
    public static IObservable<EventArgs> Observe<T>(this T component, DependencyProperty dependencyProperty)
        where T : DependencyObject
    {
        return Observable.Create<EventArgs>(observer =>
        {
            EventHandler update = (sender, args) => observer.OnNext(args);

            var property = DependencyPropertyDescriptor.FromProperty(dependencyProperty, typeof(T));
            property.AddValueChanged(component, update);
            return Disposable.Create(() => property.RemoveValueChanged(component, update));
        }).StartWithDefault();
    }
    
    public static IObservable<Unit> WhenLoaded(this FrameworkElement window)
    {
        if (window == default)
        {
            return Observable.Never<Unit>();
        }

        if (!window.CheckAccess())
        {
            return window.Dispatcher.Invoke(() => WhenLoaded(window));
        }

        if (window.IsLoaded)
        {
            return Observable.Return(Unit.Default);
        }

        return Observable
            .FromEventPattern<RoutedEventHandler, RoutedEventArgs>(h => window.Loaded += h, h => window.Loaded -= h)
            .Take(1)
            .Select(_ => Unit.Default);
    }
    
    public static DependencyObject FindVisualTreeRoot(this DependencyObject d)
    {
        var current = d;
        var result = d;

        while (current != null)
        {
            result = current;
            if (current is Visual || current is Visual3D)
            {
                break;
            }
            else
            {
                // If we're in Logical Land then we must walk 
                // up the logical tree until we find a 
                // Visual/Visual3D to get us back to Visual Land.
                current = LogicalTreeHelper.GetParent(current);
            }
        }

        return result;
    }
    
    public static IEnumerable<DependencyObject> VisualDescendants(this DependencyObject d)
    {
        var tree = new Queue<DependencyObject>();
        tree.Enqueue(d);

        while (tree.Count > 0)
        {
            var item = tree.Dequeue();
            var count = VisualTreeHelper.GetChildrenCount(item);
            for (var i = 0; i < count; ++i)
            {
                var child = VisualTreeHelper.GetChild(item, i);
                tree.Enqueue(child);
                yield return child;
            }
        }
    }
}