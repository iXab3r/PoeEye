using System.Collections.Specialized;
using System.ComponentModel;

namespace PoeShared.Scaffolding;

public interface IReadOnlyObservableCollection<out T> : IReadOnlyList<T>, INotifyCollectionChanged, INotifyPropertyChanged
{
}