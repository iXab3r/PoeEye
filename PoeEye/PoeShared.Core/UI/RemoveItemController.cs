using System.Collections.ObjectModel;
using PoeShared.Native;

namespace PoeShared.UI
{
    public sealed class RemoveItemController<T> : ICloseController
    {
        private readonly T item;
        private readonly ObservableCollection<T> collection;

        public RemoveItemController(T item, ObservableCollection<T> collection)
        {
            this.item = item;
            this.collection = collection;
        }

        public void Close()
        {
            collection.Remove(item);
        }
    }
}