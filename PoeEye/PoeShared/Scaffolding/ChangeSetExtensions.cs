using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Binding;
using DynamicData.Kernel;
using JetBrains.Annotations;


namespace PoeShared.Scaffolding
{
    public static class ChangeSetExtensions
    {
        public static ISourceList<T> ToSourceList<T>(this IObservable<IChangeSet<T>> source)
        {
            Guard.ArgumentNotNull(source, nameof(source));

            return new SourceList<T>(source);
        }

        public static ISourceList<T> ToSourceList<T>(this IEnumerable<ISourceList<T>> lists)
        {
            Guard.ArgumentNotNull(lists, nameof(lists));

            var result = new SourceList<ISourceList<T>>();
            lists.ForEach(result.Add);

            return result.Or().ToSourceList();
        }

        public static IObservable<T> WatchCurrentValue<T, TKey>(this IConnectableCache<T, TKey> cache, TKey key)
        {
            return cache.Watch(key).Select(x => x.Reason switch
            {
                ChangeReason.Add => x.Current,
                ChangeReason.Update => x.Current,
                ChangeReason.Refresh => x.Current,
                //ChangeReason.Remove x.Current contains removed element, which seems wrong
                _ => default
            });
        }

        public static void MoveItemToTop<T>(this ISourceList<T> collection, T item)
        {
            collection.Edit(list =>
            {
                var idx = list.IndexOf(item);
                if (idx < 0 || idx == 0)
                {
                    return;
                }
                list.Move(idx, 0);
            });
        }

        public static void MoveItemToBottom<T>(this ISourceList<T> collection, T item)
        {
            collection.Edit(list =>
            {
                var idx = list.IndexOf(item);
                if (idx < 0 || idx == list.Count - 1)
                {
                    return;
                }
                list.Move(idx, list.Count - 1);
            });
        }

        public static void MoveItemDown<T>(this ISourceList<T> collection, T item)
        {
            collection.Edit(list =>
            {
                var idx = list.IndexOf(item);
                if (idx < 0 || idx + 1 >= list.Count)
                {
                    return;
                }
                list.Move(idx, idx + 1);
            });
        }

        public static void MoveItemUp<T>(this ISourceList<T> collection, T item)
        {
            collection.Edit(list =>
            {
                var idx = list.IndexOf(item);
                if (idx < 0 || idx - 1 < 0)
                {
                    return;
                }
                list.Move(idx, idx - 1);
            });
        }

        public static ISourceList<T> Concat<T>(this ISourceList<T> list, params T[] items)
        {
            var newList = new SourceList<T>();
            newList.AddRange(items);
            return list.Concat(newList);
        }

        public static ISourceList<T> Concat<T>(this ISourceList<T> list, params ISourceList<T>[] lists)
        {
            return new[] { list }.Concat(lists).ToSourceList();
        }

        public static IObservable<int> CountIf<T>(this IObservable<IChangeSet<T>> source)
        {
            return source.Count();
        }

        public static void EditDiff<T, TKey>(this ISourceCache<T, TKey> source, T item)
        {
            EditDiff(source, new[] { item });
        }

        public static void EditDiff<T, TKey>(this ISourceCache<T, TKey> source, IEnumerable<T> items)
        {
            Guard.ArgumentNotNull(source, nameof(source));
            source.EditDiff(items, EqualityComparer<T>.Default);
        }

        public static Optional<T> ComputeIfAbsent<T, TKey>(this ISourceCache<T, TKey> source, TKey key, Func<TKey, T> factory)
        {
            Guard.ArgumentNotNull(source, nameof(source));

            var current = source.Lookup(key);
            if (current.HasValue)
            {
                return current;
            }

            var newValue = factory(key);
            source.AddOrUpdate(newValue);
            return Optional<T>.Create(newValue);
        }

        /// <summary>
        /// Watches each item in the collection and notifies when any of them has changed
        /// </summary>
        /// <typeparam name="TObject">The type of the object.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="propertiesToMonitor">specify properties to Monitor, or omit to monitor all property changes</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">
        /// </exception>
        public static IObservable<EventPattern<PropertyChangedEventArgs>> WhenNestedPropertyChanged<TObject>([NotNull] this IObservable<IChangeSet<TObject>> source, params string[] propertiesToMonitor)
            where TObject : INotifyPropertyChanged
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.MergeMany(t => t.WhenAnyProperty(propertiesToMonitor));
        }
        
        public static IDisposable PopulateFrom<T, TKey, T1>(this ISourceCache<T, TKey> instance, params ISourceList<T1>[] lists)
            where T1 : T
        {
            var anchors = new CompositeDisposable();
            lists.ForEach(x => SyncListWithCache(x, instance).AddTo(anchors));
            return anchors;
        }
        
        public static IDisposable PopulateFrom<T, TKey, T1, T2>(this ISourceCache<T, TKey> instance, IObservableList<T1> list1, IObservableList<T2> list2)
            where T1 : T
            where T2 : T
        {
            var anchors = new CompositeDisposable();
            SyncListWithCache(list1, instance).AddTo(anchors);
            SyncListWithCache(list2, instance).AddTo(anchors);
            return anchors;
        }
        
        public static IDisposable PopulateFrom<T, TKey, T1, T2, T3>(this ISourceCache<T, TKey> instance, IObservableList<T1> list1, IObservableList<T2> list2, IObservableList<T3> list3)
            where T1 : T
            where T2 : T
            where T3 : T
        {
            var anchors = new CompositeDisposable();
            SyncListWithCache(list1, instance).AddTo(anchors);
            SyncListWithCache(list2, instance).AddTo(anchors);
            SyncListWithCache(list3, instance).AddTo(anchors);
            return anchors;
        }
        
        public static IDisposable PopulateFrom<T, TKey, T1, T2, T3, T4>(this ISourceCache<T, TKey> instance, IObservableList<T1> list1, IObservableList<T2> list2, IObservableList<T3> list3, IObservableList<T4> list4)
            where T1 : T
            where T2 : T
            where T3 : T
            where T4 : T
        {
            var anchors = new CompositeDisposable();
            SyncListWithCache(list1, instance).AddTo(anchors);
            SyncListWithCache(list2, instance).AddTo(anchors);
            SyncListWithCache(list3, instance).AddTo(anchors);
            SyncListWithCache(list4, instance).AddTo(anchors);
            return anchors;
        }
        
        public static IDisposable PopulateFrom<T, TKey, T1, T2, T3, T4, T5>(this ISourceCache<T, TKey> instance, IObservableList<T1> list1, IObservableList<T2> list2, IObservableList<T3> list3, IObservableList<T4> list4, IObservableList<T5> list5)
            where T1 : T
            where T2 : T
            where T3 : T
            where T4 : T
            where T5 : T
        {
            var anchors = new CompositeDisposable();
            SyncListWithCache(list1, instance).AddTo(anchors);
            SyncListWithCache(list2, instance).AddTo(anchors);
            SyncListWithCache(list3, instance).AddTo(anchors);
            SyncListWithCache(list4, instance).AddTo(anchors);
            SyncListWithCache(list5, instance).AddTo(anchors);
            return anchors;
        }

        private static IDisposable SyncListWithCache<TSrc, TDst, TKey>(IObservableList<TSrc> list, ISourceCache<TDst, TKey> destination) where TSrc : TDst
        {
            var anchors = new CompositeDisposable();
            list.Connect().OnItemAdded(newObject => destination.AddOrUpdate(newObject)).Subscribe().AddTo(anchors);
            list.Connect().OnItemRemoved(newObject => destination.Remove(newObject)).Subscribe().AddTo(anchors);
            Disposable.Create(() =>
            {
                list.Items.ForEach(x => destination.Remove(x));
            }).AddTo(anchors);
            return anchors;
        }
    }
}