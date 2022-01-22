using System;
using System.Reactive.Disposables;
using PoeShared.Services;

namespace PoeShared.Scaffolding;

public static class KeyboardLayoutManagerExtensions
{
    public static IDisposable ChangeLayout(this IKeyboardLayoutManager keyboardLayoutManager, KeyboardLayout layout, IWindowHandle targetWindow)
    {
        var before = keyboardLayoutManager.GetCurrent(targetWindow);
        keyboardLayoutManager.ActivateForWindow(layout, targetWindow);
        return before == layout ? Disposable.Empty : Disposable.Create(() => keyboardLayoutManager.ActivateForWindow(before, targetWindow));
    }
}