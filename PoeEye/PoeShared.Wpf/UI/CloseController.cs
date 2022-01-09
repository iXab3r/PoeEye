using System;
using PoeShared.Scaffolding;

namespace PoeShared.UI;

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

public class CloseController<T> : ICloseController<T>
{
    private readonly Action<T> closeAction;

    public CloseController(Action<T> action)
    {
        Guard.ArgumentNotNull(action, nameof(action));
        closeAction = action;
    }

    public void Close(T value)
    {
        closeAction(value);
    }
}