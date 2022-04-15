using System.ComponentModel;
using System.Reactive;
using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Binding;
using DynamicData.Kernel;
using JetBrains.Annotations;

namespace PoeShared.Scaffolding;

public static class ChangeSetExtensions
{
    public static IObservable<IChangeSet<T>> ToObservableChangeSet<T>(this IObservableList<T> source)
    {
        return source.Connect();
    }
    
     public static IObservable<IChangeSet<T, TKey>> ToObservableChangeSet<T, TKey>(this IObservableCache<T, TKey> source)
        {
            return source.Connect();
        }
    
    public static IObservable<IChangeSet<T>> ToObservableChangeSet<T>(this IReadOnlyObservableCollection<T> source)
    {
        return source.ToObservableChangeSet<IReadOnlyObservableCollection<T>, T>();
    }
    
    public static IObservable<IChangeSet<T>> BindToCollection<T>(this IObservable<IChangeSet<T>> source, out IReadOnlyObservableCollection<T> collection)
    {
        var result = new SynchronizedObservableCollection<T>();
        collection = result;
        return source.Bind(result);
    }

    public static ISourceList<T> ToSourceList<T>(this IObservable<IChangeSet<T>> source)
    {
        Guard.ArgumentNotNull(source, nameof(source));

        return new SourceList<T>(source);
    }
    
    public static T GetOrDefault<T, TKey>(this IObservableCache<T, TKey> instance, TKey key)
    {
        if (instance.TryGetValue(key, out var value))
        {
            return value;
        }
        return default;
    }

    public static bool TryGetValue<T, TKey, TResult>(this IObservableCache<T, TKey> instance, TKey key, out TResult value, Func<T, TResult> converter)
    {
        var result = TryGetValue(instance, key, out var rawValue);
        value = result ? converter(rawValue) : default;
        return result;
    }

    public static bool TryRemove<T, TKey>(this ISourceCache<T, TKey> instance, TKey key, out T value)
    {
        var result = instance.Lookup(key);
        if (result.HasValue)
        {
            instance.RemoveKey(key);
            value = result.Value;
            return true;
        }

        value = default;
        return false;
    }
    
    public static bool TryGetValue<T, TKey>(this IObservableCache<T, TKey> instance, TKey key, out T value)
    {
        var result = instance.Lookup(key);
        if (result.HasValue)
        {
            value = result.Value;
            return true;
        }

        value = default;
        return false;
    }

    public static ISourceList<T> ToSourceList<T>(this IEnumerable<T> items)
    {
        var result = new SourceList<T>();
        result.AddRange(items);
        return result;
    }

    [Obsolete("DynamicCombiner and ReferenceCountTracker contain a bug that could be reproduced by Edit()ing a list and replacing an item there")]
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
    ///     Watches each item in the collection and notifies when any of them has changed
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
        list.Connect().OnItemRemoved(newObject => destination.Remove(newObject)).Subscribe().AddTo(anchors);
        list.Connect().OnItemAdded(newObject => destination.AddOrUpdate(newObject)).Subscribe().AddTo(anchors);
        Disposable.Create(() => { destination.Edit(destinationList => { list.Items.ForEach(x => destinationList.Remove(x)); }); }).AddTo(anchors);
        return anchors;
    }

    public static IDisposable ChangeKeyDynamically<TObject, TSourceKey, TDestinationKey>(
        this IObservable<IChangeSet<TObject, TSourceKey>> source,
        Expression<Func<TObject, TDestinationKey>> keySelectorExpression,
        out IObservableCache<TObject, TDestinationKey> cacheWithChangedKey)
        where TSourceKey : notnull
        where TDestinationKey : notnull
        where TObject : INotifyPropertyChanged
    {
        var keySelector = keySelectorExpression.Compile();
        var anchors = new CompositeDisposable();
        var result = new SourceCache<TObject, TDestinationKey>(keySelector);
        cacheWithChangedKey = result;
        source
            .WhenPropertyChanged(keySelectorExpression)
            .GroupBy(x => x.Sender)
            .Do(group =>
            {
                var item = group.Key;
                group
                    .Select(x => x.Value)
                    .WithPrevious()
                    .Subscribe(change =>
                    {
                        result.Edit(byPath =>
                        {
                            if (change.Previous != null)
                            {
                                byPath.RemoveKey(change.Previous);
                            }

                            if (change.Current != null)
                            {
                                byPath.AddOrUpdate(item);
                            }
                        });
                    }).AddTo(anchors);
            })
            .Subscribe()
            .AddTo(anchors);

        source
            .OnItemRemoved(x => { result.Remove(x); })
            .Subscribe()
            .AddTo(anchors);

        return anchors;
    }

    public static IObservable<IChangeSet<TDestination, TKey>> TransformWithInlineUpdate<TObject, TKey, TDestination>(this IObservable<IChangeSet<TObject, TKey>> source,
        Func<TObject, TDestination> transformFactory,
        Action<TDestination, TObject> updateAction = null)
    {
        return source.Scan((ChangeAwareCache<TDestination, TKey>)null, (cache, changes) =>
        {
            //The change aware cache captures a history of all changes so downstream operators can replay the changes
            if (cache == null)
            {
                cache = new ChangeAwareCache<TDestination, TKey>(changes.Count);
            }

            foreach (var change in changes)
            {
                switch (change.Reason)
                {
                    case ChangeReason.Add:
                        cache.AddOrUpdate(transformFactory(change.Current), change.Key);
                        break;
                    case ChangeReason.Update:
                    {
                        if (updateAction == null)
                        {
                            continue;
                        }

                        var previous = cache.Lookup(change.Key)
                            .ValueOrThrow(() => new MissingKeyException($"{change.Key} is not found."));
                        //callback when an update has been received
                        updateAction(previous, change.Current);

                        //send a refresh as this will force downstream operators to filter, sort, group etc
                        cache.Refresh(change.Key);
                    }
                        break;
                    case ChangeReason.Remove:
                        cache.Remove(change.Key);
                        break;
                    case ChangeReason.Refresh:
                        cache.Refresh(change.Key);
                        break;
                    case ChangeReason.Moved:
                        //Do nothing !
                        break;
                }
            }

            return cache;
        }).Select(cache => cache.CaptureChanges()); //invoke capture changes to return the changeset
    }
}