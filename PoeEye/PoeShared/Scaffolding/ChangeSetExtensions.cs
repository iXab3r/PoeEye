using System.Collections.Specialized;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Concurrency;
using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Binding;
using DynamicData.Kernel;
using JetBrains.Annotations;

namespace PoeShared.Scaffolding;

public static class ChangeSetExtensions
{
    /// <summary>
    /// Automatically refresh downstream operator. The refresh is triggered when the observable receives a notification.
    /// </summary>
    /// <typeparam name="TObject">The type of object.</typeparam>
    /// <typeparam name="TAny">A ignored type used for specifying what to auto refresh on.</typeparam>
    /// <param name="source">The source observable change set.</param>
    /// <param name="reevaluator">An observable which acts on items within the collection and produces a value when the item should be refreshed.</param>
    /// <param name="changeSetBuffer">Batch up changes by specifying the buffer. This greatly increases performance when many elements require a refresh.</param>
    /// <param name="scheduler">The scheduler.</param>
    /// <returns>An observable change set with additional refresh changes.</returns>
    public static IObservable<IChangeSet<TObject>> AutoRefreshOnObservableSynchronized<TObject, TAny>(
        this IObservable<IChangeSet<TObject>> source, 
        Func<TObject, IObservable<TAny>> reevaluator, TimeSpan? changeSetBuffer = null, IScheduler scheduler = null)
    {
        var gate = new object();
        return source.Synchronize(gate).AutoRefreshOnObservable(o => reevaluator(o).Synchronize(gate), changeSetBuffer, scheduler);
    }

    /// <summary>
    /// Automatically refresh downstream operator. The refresh is triggered when the observable receives a notification.
    /// </summary>
    /// <typeparam name="TObject">The object of the change set.</typeparam>
    /// <typeparam name="TKey">The key of the change set.</typeparam>
    /// <typeparam name="TAny">The type of evaluation.</typeparam>
    /// <param name="source">The source observable change set.</param>
    /// <param name="reevaluator">An observable which acts on items within the collection and produces a value when the item should be refreshed.</param>
    /// <param name="changeSetBuffer">Batch up changes by specifying the buffer. This greatly increases performance when many elements require a refresh.</param>
    /// <param name="scheduler">The scheduler.</param>
    /// <returns>An observable change set with additional refresh changes.</returns>
    public static IObservable<IChangeSet<TObject, TKey>> AutoRefreshOnObservableSynchronized<TObject, TKey, TAny>(this IObservable<IChangeSet<TObject, TKey>> source, Func<TObject, IObservable<TAny>> reevaluator, TimeSpan? changeSetBuffer = null, IScheduler scheduler = null)
        where TKey : notnull
    {
        return source.AutoRefreshObservableSynchronized(reevaluator, changeSetBuffer, scheduler);
    }

    /// <summary>
    /// Automatically refresh downstream operator. The refresh is triggered when the observable receives a notification.
    /// </summary>
    /// <typeparam name="TObject">The object of the change set.</typeparam>
    /// <typeparam name="TKey">The key of the change set.</typeparam>
    /// <typeparam name="TAny">The type of evaluation.</typeparam>
    /// <param name="source">The source observable change set.</param>
    /// <param name="reevaluator">An observable which acts on items within the collection and produces a value when the item should be refreshed.</param>
    /// <param name="changeSetBuffer">Batch up changes by specifying the buffer. This greatly increases performance when many elements require a refresh.</param>
    /// <param name="scheduler">The scheduler.</param>
    /// <returns>An observable change set with additional refresh changes.</returns>
    public static IObservable<IChangeSet<TObject, TKey>> 
        AutoRefreshObservableSynchronized<TObject, TKey, TAny>(
            this IObservable<IChangeSet<TObject, TKey>> source, 
            Func<TObject, IObservable<TAny>> reevaluator, 
            TimeSpan? changeSetBuffer = null, 
            IScheduler scheduler = null)
        where TKey : notnull
    {
        var gate = new object();
        return source.Synchronize(gate).AutoRefreshOnObservable((o, key) => reevaluator(o).Synchronize(gate), changeSetBuffer, scheduler);
    }
    
    public static T EditGet<TItem, T>(this ISourceList<TItem> source, Func<IExtendedList<TItem>, T> supplier)
    {
        T result = default;
        source.Edit(list => result = supplier(list));
        return result;
    }
    
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

    public static IObservable<IChangeSet<TValue>> BindToCollection<TKey, TValue>(this IObservable<IChangeSet<TValue, TKey>> source, out IReadOnlyObservableCollection<TValue> collection)
    {
        return source.RemoveKey().BindToCollection(out collection);
    }
    
    public static IObservable<NotifyCollectionChangedEventArgs> ToNotifyCollectionChanged<T>(this IObservable<IChangeSet<T>> source)
    {
        return Observable.Create<NotifyCollectionChangedEventArgs>(observer =>
        {

            var anchors = new CompositeDisposable();
            source
                .ForEachChange(x =>
                {
                    NotifyCollectionChangedEventArgs changedEventArgs;
                    if (x.Type == ChangeType.Range && x.Reason is not ListChangeReason.Clear or ListChangeReason.Refresh)
                    {
                        throw new ArgumentOutOfRangeException();
                    }
                    switch (x.Reason)
                    {
                        case ListChangeReason.Add:
                            changedEventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new[] {x.Item.Current}, x.Item.CurrentIndex);
                            break;
                        case ListChangeReason.Replace:
                            changedEventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, new[] {x.Item.Current}, new[] {x.Item.Previous.Value}, x.Item.CurrentIndex);
                            break;
                        case ListChangeReason.Remove:
                            changedEventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new[] { x.Item.Current }, x.Item.CurrentIndex);
                            break;
                        case ListChangeReason.Clear:
                            changedEventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    observer.OnNext(changedEventArgs);
                })
                .Subscribe()
                .AddTo(anchors);
            
            return anchors;
        });
    }
    
    public static IObservable<IChangeSet<T>> BindToCollection<T>(this IObservable<IChangeSet<T>> source, out IReadOnlyObservableCollection<T> collection)
    {
        var result = new ObservableCollectionEx<T>();
        collection = result;
        return source.Bind(result, resetThreshold: int.MaxValue); // never reset to avoid breaking PropertyBinder
    }
    
    public static IObservable<IChangeSet<T>> BindToCollectionSynchronized<T>(this IObservable<IChangeSet<T>> source, out IReadOnlyObservableCollection<T> collection)
    {
        var result = new SynchronizedObservableCollectionEx<T>();
        collection = result;
        return source.Bind(result, resetThreshold: int.MaxValue); // never reset to avoid breaking PropertyBinder
    }

    public static ISourceListEx<T> ToSourceListEx<T>(this IObservable<IChangeSet<T>> source)
    {
        Guard.ArgumentNotNull(source, nameof(source));

        return new SourceListEx<T>(source); 
    }
    
    public static ISourceList<T> ToSourceList<T>(this IObservable<IChangeSet<T>> source)
    {
        return source.ToSourceListEx();
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
    
    public static IObservable<IChangeSet<TOut, TKey>> SwitchCollectionIf<TIn, TOut, TKey>(
        this IObservable<TIn> observable,
        [NotNull] Predicate<TIn> condition,
        [NotNull] Func<TIn, IObservableCache<TOut, TKey>> trueSelector,
        [NotNull] Func<TIn, IObservableCache<TOut, TKey>> falseSelector)
    {
        return observable
            .Select(x => condition(x) ? trueSelector(x) : falseSelector(x))
            .Switch();
    }
    
    public static IObservable<IChangeSet<TOut>> SwitchCollectionIf<TIn, TOut>(
        this IObservable<TIn> observable,
        [NotNull] Predicate<TIn> condition,
        [NotNull] Func<TIn, IObservableList<TOut>> trueSelector,
        [NotNull] Func<TIn, IObservableList<TOut>> falseSelector)
    {
        return observable
            .Select(x => condition(x) ? trueSelector(x) : falseSelector(x))
            .Switch();
    }

    public static IObservable<IChangeSet<TOut>> SwitchCollectionIf<TIn, TOut>(
        this IObservable<TIn> observable,
        [NotNull] Predicate<TIn> condition,
        [NotNull] Func<TIn, IObservableList<TOut>> trueSelector)
    {
        return SwitchCollectionIf(observable,  condition, trueSelector, x => new SourceListEx<TOut>());
    }
    
    public static IObservable<IChangeSet<TOut, TKey>> SwitchCollectionIf<TIn, TOut, TKey>(
        this IObservable<TIn> observable,
        [NotNull] Predicate<TIn> condition,
        [NotNull] Func<TIn, IObservableCache<TOut, TKey>> trueSelector)
    {
        return SwitchCollectionIf(observable,  condition, trueSelector, x => new IntermediateCache<TOut, TKey>());
    }
    
    public static IObservable<IChangeSet<TOut, TKey>> SwitchCollectionIfNotDefault<TIn, TOut, TKey>(
        this IObservable<TIn> observable,
        [NotNull] Func<TIn, IObservableCache<TOut, TKey>> trueSelector)
    {
        return SwitchCollectionIf(observable, x => !EqualityComparer<TIn>.Default.Equals(default, x), trueSelector);
    }

    public static IObservable<IChangeSet<TOut>> SwitchCollectionIfNotDefault<TIn, TOut>(
        this IObservable<TIn> observable,
        [NotNull] Func<TIn, IObservableList<TOut>> trueSelector)
    {
        return SwitchCollectionIf(observable, x => !EqualityComparer<TIn>.Default.Equals(default, x), trueSelector);
    }
    
    public static T GetOrAdd<T, TKey>(
        this ISourceCache<T, TKey> instance, 
        TKey key,
        Func<TKey, T> factoryFunc)
    {
        var result = Optional<T>.None;
        instance.Edit(items =>
        {
            var existingItem = items.Lookup(key);
            if (existingItem.HasValue)
            {
                result = existingItem;
                return;
            }
                    
            var newItem = factoryFunc(key);
            result = Optional<T>.Create(newItem);
        });
        if (!result.HasValue)
        {
            throw new InvalidStateException($"Failed to get or add new item for key {key}");
        }
        return result.Value;
    }

    public static ISourceListEx<T> ToSourceListEx<T>(this IEnumerable<T> items)
    {
        return new SourceListEx<T>(items.ToSourceList());
    }
    
    public static ISourceList<T> ToSourceList<T>(this IEnumerable<T> items)
    {
        var result = new SourceListEx<T>();
        result.AddRange(items);
        return result;
    }

    [Obsolete("DynamicCombiner and ReferenceCountTracker contain a bug that could be reproduced by Edit()ing a list and replacing an item there")]
    public static ISourceList<T> ToSourceList<T>(this IEnumerable<ISourceList<T>> lists)
    {
        Guard.ArgumentNotNull(lists, nameof(lists));

        var result = new SourceListEx<ISourceList<T>>();
        lists.ForEach(result.Add);

        return result.Or().ToSourceList();
    }
    
    public static IObservable<T> WatchCurrentValue<T, TKey>(this IObservable<Change<T, TKey>> events)
    {
        return events.Select(x => x.Reason switch
        {
            ChangeReason.Add => x.Current,
            ChangeReason.Update => x.Current,
            ChangeReason.Refresh => x.Current,
            //ChangeReason.Remove x.Current contains removed element, which seems wrong
            _ => default
        });
    }

    public static IObservable<T> WatchCurrentValue<T, TKey>(this IObservable<IChangeSet<T, TKey>> events, TKey key)
    {
        return events.Watch(key).WatchCurrentValue();
    }
    
    public static IObservable<T> WatchCurrentValue<T, TKey>(this IObservableCache<T, TKey> cache, TKey key)
    {
        var result = cache.Watch(key).WatchCurrentValue();
        return cache.Lookup(key).HasValue ? result : result.StartWithDefault();
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
        var newList = new SourceListEx<T>();
        newList.AddRange(items);
        return list.Concat(newList);
    }

    public static ISourceList<T> Concat<T>(this ISourceList<T> list, params ISourceList<T>[] lists)
    {
#pragma warning disable CS0618 This is currently the only way
        return new[] { list }.Concat(lists).ToSourceList();
#pragma warning restore CS0618
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