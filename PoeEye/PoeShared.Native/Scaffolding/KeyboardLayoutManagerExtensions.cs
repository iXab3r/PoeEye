using System;
using System.Globalization;
using System.Reactive.Disposables;
using PoeShared.Services;

namespace PoeShared.Scaffolding
{
    public static class KeyboardLayoutManagerExtensions
    {
        public static IDisposable ChangeLayout(this IKeyboardLayoutManager keyboardLayoutManager, KeyboardLayout layout)
        {
            var before = keyboardLayoutManager.GetCurrent();
            keyboardLayoutManager.Activate(layout);
            return Disposable.Create(() => keyboardLayoutManager.Activate(before));
        }
    }
}