using System.Collections.Specialized;
using DynamicData;

namespace PoeShared.Scaffolding;

public interface ISourceListEx<T> : ISourceList<T>, IObservableListEx<T>, IEnumerable<T>, IDisposableReactiveObject, INotifyCollectionChanged
{
}

public interface IObservableListEx<T> : IObservableList<T>
{
    public IReadOnlyObservableCollection<T> Collection { get; }
}