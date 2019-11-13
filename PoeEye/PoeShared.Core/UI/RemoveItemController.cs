using System;
using System.Collections.ObjectModel;
using DynamicData;
using PoeShared.Native;

namespace PoeShared.UI
{
    public sealed class RemoveItemController<T> : ICloseController
    {
        private readonly T item;
        private readonly Action closeAction;

        public RemoveItemController(T item, ObservableCollection<T> collection) : this(item, () => collection.Remove(item))
        {
        }
        
        public RemoveItemController(T item, ISourceList<T> collection) : this(item, () => collection.Remove(item))
        {
        }
        
        public RemoveItemController(T item, Action action)
        {
            Guard.ArgumentNotNull(action, nameof(action));
            this.item = item;
            closeAction = action;
        }

        public void Close()
        {
            closeAction();
            if (item is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}