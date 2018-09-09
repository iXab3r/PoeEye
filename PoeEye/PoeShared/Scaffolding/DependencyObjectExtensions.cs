﻿using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;

namespace PoeShared.Scaffolding
{
    public static class DependencyObjectExtensions
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
            });
        }
    }
}