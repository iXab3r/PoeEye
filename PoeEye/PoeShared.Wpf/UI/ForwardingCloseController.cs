using PoeShared.Scaffolding;

namespace PoeShared.UI;

public sealed class ForwardingCloseController<T> : ICloseController<T>
{
    private readonly ICloseController<T>[] controllers;

    public ForwardingCloseController(params ICloseController<T>[] controllers)
    {
        this.controllers = controllers;
    }

    public void Close(T value)
    {
        foreach (var closeController in controllers)
        {
            closeController.Close(value);
        }
    }
}