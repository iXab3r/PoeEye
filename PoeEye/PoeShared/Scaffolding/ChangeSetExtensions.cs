using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
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

        public static void AddOrUpdateIfNeeded<T, TKey>(this ISourceCache<T, TKey> source, T item)
        {
            AddOrUpdateIfNeeded(source, new[] { item });
        }
        
        public static void AddOrUpdateIfNeeded<T, TKey>(this ISourceCache<T, TKey> source, IEnumerable<T> items)
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
    }
}