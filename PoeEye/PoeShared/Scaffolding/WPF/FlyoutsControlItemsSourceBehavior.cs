using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;
using log4net.Filter;
using MahApps.Metro.Controls;

namespace PoeShared.Scaffolding.WPF
{
    public sealed class FlyoutsControlItemsSourceBehavior : Behavior<ItemsControl>
    {
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                "ItemsSource",
                typeof(object),
                typeof(FlyoutsControlItemsSourceBehavior),
                new UIPropertyMetadata(new List<object>(), OnItemsSourcePropertyChangedCallback));

        public object ItemsSource
        {
            get { return (object) GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        private readonly SerialDisposable itemsSourceSubscription = new SerialDisposable();

        protected override void OnDetaching()
        {
            itemsSourceSubscription.Dispose();
            base.OnDetaching();
        }

        private static void OnItemsSourcePropertyChangedCallback(
            DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var behavior = dependencyObject as FlyoutsControlItemsSourceBehavior;
            var newItemsSource = dependencyPropertyChangedEventArgs.NewValue as IEnumerable;
            behavior?.SubscribeToItemsSource(newItemsSource);
        }

        private void SubscribeToItemsSource(IEnumerable itemsSource)
        {
            var itemsSourceAnchors = new CompositeDisposable();
            itemsSourceSubscription.Disposable = itemsSourceAnchors;

            var npcSource = itemsSource as INotifyCollectionChanged;
            if (npcSource == null)
            {
                return;
            }

            Observable
                .FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>
                (h => npcSource.CollectionChanged += h, h => npcSource.CollectionChanged -= h)
                .Subscribe(x => ProcessCollectionChange(itemsSource, x.EventArgs))
                .AddTo(itemsSourceAnchors);
            ProcessCollectionChange(
                itemsSource, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        private void ProcessCollectionChange(IEnumerable itemsSource, NotifyCollectionChangedEventArgs args)
        {
            if (args == null)
            {
                return;
            }

            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var argsNewItem in args?.NewItems)
                    {
                        AssociatedObject.Items?.Add(argsNewItem);
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    foreach (var argsNewItem in itemsSource)
                    {
                        AssociatedObject.Items?.Add(argsNewItem);
                    }
                    break;
            }
        }
    }
}