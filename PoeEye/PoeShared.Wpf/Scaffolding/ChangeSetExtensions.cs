using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using PoeShared.Logging;
using PoeShared.Prism;
using PoeShared.UI;

namespace PoeShared.Scaffolding;

public static class ChangeSetExtensions
{
    private static readonly IFluentLog Log = typeof(ChangeSetExtensions).PrepareLogger();

    public static IObservable<IChangeSet<T>> BindToCollectionVirtualized<T>(
        this IObservable<IChangeSet<T>> source,
        out IReadOnlyObservableCollection<IVirtualizedListContainer<T>> collection) where T : class
    {
        return BindToCollectionVirtualized(source, () => new VirtualizedListContainer<T>(), out collection);
    }

    public static IObservable<IChangeSet<T>> BindToCollectionVirtualized<T, TContainer>(
        this IObservable<IChangeSet<T>> source,
        Func<TContainer> containerFactory,
        out IReadOnlyObservableCollection<TContainer> collection) where T : class where TContainer : IVirtualizedListContainer<T>
    {
        var resultCollection = new ObservableCollectionEx<TContainer>();
        collection = resultCollection;

        return Observable.Create<IChangeSet<T>>(observer =>
        {
            var anchors = new CompositeDisposable();
            var list = new VirtualizedList<T, TContainer>(
                source,
                containerFactory: new LambdaFactory<TContainer>(containerFactory)).AddTo(anchors);
            list.Containers.Connect().Bind(resultCollection).SubscribeToErrors(Log.HandleUiException).AddTo(anchors);
            return anchors;
        });
    }

    public static IObservable<Unit> BindToSourceList<TCollection, T>(
        this TCollection source,
        ISourceList<T> destination) where TCollection : INotifyCollectionChanged, IEnumerable<T>, ICollection<T>
    {
        //FIXME This will NOT sync if changes are done on destination list
        return Observable.Create<Unit>(observer =>
        {
            var anchors = new CompositeDisposable();

            source.Clear();
            destination.Items.ForEach(source.Add);
                
            source
                .ToObservableChangeSet<TCollection, T>(skipInitial: true)
                .ForEachItemChange(change =>
                {
                    switch (change.Reason)
                    {
                        case ListChangeReason.Add:
                            destination.Add(change.Current);
                            break;
                        case ListChangeReason.Remove:
                            destination.Remove(change.Current);
                            break;
                        case ListChangeReason.Replace:
                            destination.Replace(change.Previous.Value, change.Current);
                            break;
                        case ListChangeReason.Clear:
                            destination.Clear();
                            break;
                        case ListChangeReason.Moved:
                            destination.Move(change.PreviousIndex, change.CurrentIndex);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(change), change.Reason, $"Change of type {change.Reason} is not supported, full change: {change}");
                    }
                })
                .SubscribeToErrors(Log.HandleUiException)
                .AddTo(anchors);


            return anchors;
        });
    }

    public static IObservable<IChangeSet<T>> ToObservableChangeSet<TCollection, T>(
        this TCollection source,
        bool skipInitial)
        where TCollection : INotifyCollectionChanged, IEnumerable<T>
    {
        if (!skipInitial || source.IsEmpty())
        {
            return source.ToObservableChangeSet<TCollection, T>();
        }
        //FIXME There is a bug - SkipInitial from DynamicData skips first element if collection was empty at the beginning
        return source.ToObservableChangeSet<TCollection, T>().SkipInitial();
    }
    
    public static IObservable<IChangeSet<T>> ToObservableChangeSet<T>(
        this IReadOnlyObservableCollection<T> source,
        bool skipInitial)
    {
        if (!skipInitial || source.IsEmpty())
        {
            return source.ToObservableChangeSet();
        }
        //FIXME There is a bug - SkipInitial from DynamicData skips first element if collection was empty at the beginning
        return source.ToObservableChangeSet().SkipInitial();
    }
    
    public static IObservable<IChangeSet<T>> ToObservableChangeSet<T>(
        this IObservableList<T> source,
        bool skipInitial)
    {
        if (!skipInitial || source.Count == 0)
        {
            return source.Connect();
        }
        //FIXME There is a bug - SkipInitial from DynamicData skips first element if collection was empty at the beginning
        return source.Connect().SkipInitial();
    }
    
    public static IObservable<IChangeSet<TObject, TKey>> ToObservableChangeSet<TObject, TKey>(
        this IObservableCache<TObject, TKey> source,
        bool skipInitial)
    {
        if (!skipInitial || source.Count == 0)
        {
            return source.Connect();
        }
        //FIXME There is a bug - SkipInitial from DynamicData skips first element if collection was empty at the beginning
        return source.Connect().SkipInitial();
    }
}