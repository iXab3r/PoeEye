using System;
using PoeShared.Native;

namespace PoeShared.UI
{
    public class CloseController : ICloseController
    {
        private readonly Action closeAction;

        public CloseController(Action action)
        {
            Guard.ArgumentNotNull(action, nameof(action));
            closeAction = action;
        }

        public virtual void Close()
        {
            closeAction();
        }
    }
    
    public class CloseController<T> : CloseController
    {
        private readonly T item;

        public CloseController(T item, Action action) : base(action)
        {
            this.item = item;
        }

        public override void Close()
        {
            base.Close();
            if (item is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}