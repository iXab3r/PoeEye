using System;
using PoeShared.Native;
using PoeShared.Scaffolding;

namespace PoeShared.UI
{
    public class ParameterizedCloseController<T, TValue> : ICloseController<TValue>
    {
        private readonly T item;
        private readonly Action<TValue> closeAction;

        public ParameterizedCloseController(T item, Action<TValue> action)
        {
            Guard.ArgumentNotNull(action, nameof(action));
            this.item = item;
            closeAction = action;
        }

        public void Close(TValue value)
        {
            closeAction(value);
            if (item is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}