using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace PoeShared.Scaffolding
{
    public static class DependencyObjectExtensions
    {
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
                dependencyObject1 = dependencyObject1 is Visual || dependencyObject1 is Visual3D
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
    }
}