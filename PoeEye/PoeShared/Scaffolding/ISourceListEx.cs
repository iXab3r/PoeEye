using System.Collections.Specialized;
using DynamicData;

namespace PoeShared.Scaffolding;

public interface ISourceListEx<T> : ISourceList<T>, IEnumerable<T>, IDisposableReactiveObject, INotifyCollectionChanged
{
}