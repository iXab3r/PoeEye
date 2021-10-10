using System;

namespace WindowsHook
{
    public interface ISupportsMouseFilter
    {
        IDisposable AddMouseFilter(IMouseEventFilter filter);
    }
}