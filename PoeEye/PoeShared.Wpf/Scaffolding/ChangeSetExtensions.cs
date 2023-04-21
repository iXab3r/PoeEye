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
        var resultCollection = new ObservableCollectionEx<VirtualizedListContainer<T>>();
        collection = resultCollection;
        
        return Observable.Create<IChangeSet<T>>(observer =>
        {
            var anchors = new CompositeDisposable();
            var list = new VirtualizedList<T, VirtualizedListContainer<T>>(
                source,
                containerFactory: new LambdaFactory<VirtualizedListContainer<T>>(() => new VirtualizedListContainer<T>())).AddTo(anchors);
            list.Containers.Connect().Bind(resultCollection).Subscribe().AddTo(anchors);
            return anchors;
        });
    }
    
    
}