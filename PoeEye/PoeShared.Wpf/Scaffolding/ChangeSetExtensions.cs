using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using PoeShared.Prism;
using PoeShared.UI;

namespace PoeShared.Scaffolding;

public static class ChangeSetExtensions
{
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
            list.Containers.Connect().Bind(resultCollection).Subscribe().AddTo(anchors);
            return anchors;
        });
    }
    
    public static IObservable<Unit> SynchronizeToSourceList<TCollection, T>(
        this TCollection source,
        ISourceList<T> destination)  where TCollection : INotifyCollectionChanged, IEnumerable<T>, ICollection<T>
    {
        //FIXME This will NOT sync if changes are done on destination list
        return Observable.Create<Unit>(observer =>
        {
            var anchors = new CompositeDisposable();

            source.Clear();
            destination.Items.ForEach(source.Add);

            source
                .ToObservableChangeSet<TCollection, T>()
                .SkipInitial()
                .ForEachItemChange(change =>
                {
                    switch (change.Reason)
                    {
                        case ListChangeReason.Add:
                            destination.Insert(change.CurrentIndex, change.Current);
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
                .Subscribe()
                .AddTo(anchors);


            return anchors;
        });
    }
}