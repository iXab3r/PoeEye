using System.Collections.Specialized;
using DynamicData;

namespace PoeShared.Scaffolding;

public interface ISourceCacheEx<T, TKey> : ISourceCache<T, TKey>, IEnumerable<T>, IObservableCacheEx<T, TKey>, IDisposableReactiveObject, INotifyCollectionChanged
{
}

public interface IObservableCacheEx<T, TKey> : IObservableCache<T, TKey>
{
    public IReadOnlyObservableCollection<T> Collection { get; }
}