using DynamicData;

namespace PoeShared.Scaffolding;

public interface IHierarchicalSourceCache<TObject, TKey> : ISourceCache<TObject, TKey>, IDisposableReactiveObject
{
    IObservableCache<TObject, TKey> LocalCache { get; }

    IHierarchicalSourceCache<TObject, TKey> Parent { get; set; }
}