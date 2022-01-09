using System;
using PoeShared.Native;
using PoeShared.Logging;

namespace PoeShared.UI;

public class ItemCloseController<T> : CloseController
{
    private readonly T item;

    public ItemCloseController(Action action) : this(default, action)
    {
    }

    public ItemCloseController(T item, Action action) : base(action)
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