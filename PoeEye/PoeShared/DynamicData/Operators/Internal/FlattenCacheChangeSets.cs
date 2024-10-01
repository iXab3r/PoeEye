using DynamicData;
using PoeShared.Services;

namespace PoeShared.DynamicData.Operators.Internal;

internal sealed class FlattenCacheChangeSets
{
    private static long globalCounter;
    
    public IObservable<IChangeSet<TObject, TKey>> Run<TObject, TKey>(
        IObservable<IChangeSet<TObject, TKey>> source,
        Func<TObject, IObservable<IChangeSet<TObject, TKey>>> childrenAccessor,
        Func<TObject, TKey> keySelector)
    {
        return Observable.Create<IChangeSet<TObject, TKey>>(observer =>
        {
            var gate = new NamedLock($"FlattenCacheChangeSetsLock#{Interlocked.Increment(ref globalCounter)}");
            var anchors = new CompositeDisposable();

            var intermediateCache = new SourceCache<TObject, TKey>(keySelector).AddTo(anchors);

            source
                .Synchronize(gate)
                .Transform(x => new ChildrenExtractor<TObject, TKey>(gate, intermediateCache, x, childrenAccessor))
                .DisposeMany()
                .Subscribe()
                .AddTo(anchors);

            intermediateCache.Connect().Subscribe(observer).AddTo(anchors);
            return anchors;
        });
    }

    private sealed class ChildrenExtractor<TObject, TKey> : DisposableReactiveObject
    {
        public ChildrenExtractor(
            NamedLock gate,
            SourceCache<TObject, TKey> intermediateCache, 
            TObject parent, 
            Func<TObject, IObservable<IChangeSet<TObject, TKey>>> childrenAccessor)
        {
            Parent = parent;

            intermediateCache.AddOrUpdate(parent);

            var changes = childrenAccessor(parent);
            changes
                .Synchronize(gate)
                .OnItemRemoved(x =>
                {
                    intermediateCache.Remove(x);
                })
                .OnItemAdded(x =>
                {
                    intermediateCache.AddOrUpdate(x);
                })
                .OnItemUpdated((prev, curr) =>
                {
                    intermediateCache.AddOrUpdate(curr);
                }).Subscribe()
                .AddTo(Anchors);

            Disposable.Create(() =>
            {
                intermediateCache.Remove(parent);
            }).AddTo(Anchors);
        }
        
        public TObject Parent { get; }
    }
}