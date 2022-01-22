using System.Collections.ObjectModel;
using DynamicData;

namespace PoeShared.UI;

public sealed class RemoveItemController<T> : ItemCloseController<T>
{
    public RemoveItemController(T item, ObservableCollection<T> collection) : base(item, () => collection.Remove(item))
    {
    }
        
    public RemoveItemController(T item, ISourceList<T> collection) : base(item, () => collection.Remove(item))
    {
    }
}