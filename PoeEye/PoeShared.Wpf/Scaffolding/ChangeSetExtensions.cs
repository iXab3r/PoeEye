using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
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

}