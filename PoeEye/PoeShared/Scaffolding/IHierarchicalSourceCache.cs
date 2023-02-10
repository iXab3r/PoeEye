using DynamicData;

namespace PoeShared.Scaffolding;

public interface IHierarchicalSourceCache<TObject, TKey> : ISourceCache<TObject, TKey>, IDisposableReactiveObject
{
    IHierarchicalSourceCache<TObject, TKey> Parent { get; set; }
}