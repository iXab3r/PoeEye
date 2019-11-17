using System;
using PoeShared.Native;

namespace PoeShared.UI
{
    public class CloseController<T> : ICloseController
    {
        private readonly T item;
        private readonly Action closeAction;

        public CloseController(T item, Action action)
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