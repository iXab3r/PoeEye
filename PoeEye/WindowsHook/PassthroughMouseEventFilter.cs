using System;

namespace WindowsHook;

internal sealed class PassthroughMouseEventFilter : IMouseEventFilter
{
    private static readonly Lazy<IMouseEventFilter> InstanceSupplier = new(() => new PassthroughMouseEventFilter());

    public static IMouseEventFilter Instance => InstanceSupplier.Value;

    public bool ShouldProcess(MouseEventExtArgs eventArgs)
    {
        return true;
    }
}