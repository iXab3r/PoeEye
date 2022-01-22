using System;

namespace WindowsHook;

public interface ISupportsKeyboardFilter
{
    IDisposable AddKeyboardFilter(IKeyboardEventFilter filter);
}