using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace PoeShared.Scaffolding;

public static class DependencyObjectExtensions
{
    /// <summary>
    /// Returns full visual ancestry, starting at the leaf.
    /// <para>If element is not of <see cref="Visual"/> or <see cref="Visual3D"/> the
    /// logical ancestry is used.</para>
    /// </summary>
    /// <param name="leaf"></param>
    /// <returns></returns>
    public static IEnumerable<DependencyObject> GetVisualAncestry(this DependencyObject? leaf)
    {
        while (leaf is not null)
        {
            yield return leaf;
            leaf = leaf is Visual || leaf is Visual3D
                ? VisualTreeHelper.GetParent(leaf)
                : LogicalTreeHelper.GetParent(leaf);
        }
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

    public static IEnumerable<DependencyObject> VisualAncestors(this DependencyObject d)
    {
        var parent = VisualTreeHelper.GetParent(d);
        while (parent != null)
        {
            yield return parent;
            parent = VisualTreeHelper.GetParent(parent);
        }
    }

    public static void SetCurrentValueIfChanged<T, TValue>(
        this T component, DependencyProperty dependencyProperty,
        TValue value)
        where T : DependencyObject
    {
        var currentValueUntyped = component.GetValue(dependencyProperty);
        if (currentValueUntyped is not TValue currentValue || !EqualityComparer<TValue>.Default.Equals(currentValue, value))
        {
            component.SetCurrentValue(dependencyProperty, value);
        }
    }

    public static IObservable<TValue> Observe<T, TValue>(this T component, DependencyProperty dependencyProperty, Func<T, TValue> selector)
        where T : DependencyObject
    {
        return Observe(component, dependencyProperty)
            .Select(_ => selector(component));
    }

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

    public static IObservable<TValue> Observe<T, TValue>(this T component, DependencyProperty dependencyProperty)
        where T : DependencyObject
    {
        return Observe(component, dependencyProperty).Select(_ => component.GetValue(dependencyProperty)).OfType<TValue>();
    }

    public static IEnumerable<T> FindVisualChildren<T>(this DependencyObject depObj) where T : DependencyObject
    {
        if (depObj == null)
        {
            yield break;
        }

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); ++i)
        {
            var child = VisualTreeHelper.GetChild(depObj, i);
            if (child is T variable)
            {
                yield return variable;
            }

            foreach (var visualChild in child.FindVisualChildren<T>())
            {
                yield return visualChild;
            }

            child = null;
        }
    }

    public static IEnumerable<T> FindLogicalChildren<T>(this DependencyObject depObj) where T : DependencyObject
    {
        if (depObj == null)
        {
            yield break;
        }

        foreach (var dependencyObject in LogicalTreeHelper.GetChildren(depObj).OfType<DependencyObject>())
        {
            var child = dependencyObject;
            if (child is T variable)
            {
                yield return variable;
            }

            foreach (var logicalChild in child.FindLogicalChildren<T>())
            {
                yield return logicalChild;
            }

            child = null;
        }
    }

    public static DependencyObject FindVisualTreeRoot(this DependencyObject initial)
    {
        var dependencyObject1 = initial;
        var dependencyObject2 = initial;
        for (;
             dependencyObject1 != null;
             dependencyObject1 = dependencyObject1 is Visual or Visual3D
                 ? VisualTreeHelper.GetParent(dependencyObject1)
                 : LogicalTreeHelper.GetParent(dependencyObject1))
        {
            dependencyObject2 = dependencyObject1;
        }

        return dependencyObject2;
    }

    public static T FindVisualAncestor<T>(this DependencyObject dependencyObject) where T : class
    {
        var reference = dependencyObject;
        do
        {
            reference = VisualTreeHelper.GetParent(reference);
        } while (reference != null && !(reference is T));

        return reference as T;
    }

    public static T FindLogicalAncestor<T>(this DependencyObject dependencyObject) where T : class
    {
        var current = dependencyObject;
        do
        {
            var reference = current;
            current = LogicalTreeHelper.GetParent(current) ?? VisualTreeHelper.GetParent(reference);
        } while (current != null && !(current is T));

        return current as T;
    }

    public static IEnumerable<DependencyObject> VisualAncestorsAndSelf(this DependencyObject obj)
    {
        while (obj != null)
        {
            yield return obj;
            if (obj is Visual or Visual3D)
            {
                obj = VisualTreeHelper.GetParent(obj);
            }
            else if (obj is FrameworkContentElement element)
            {
                // When called with a non-visual such as a TextElement, walk up the
                // logical tree instead.
                obj = element.Parent;
            }
            else
            {
                break;
            }
        }
    }
}