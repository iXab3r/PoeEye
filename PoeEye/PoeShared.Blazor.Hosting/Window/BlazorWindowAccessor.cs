using System;
using PoeShared.Services;

namespace PoeShared.Blazor.Wpf;

internal sealed class BlazorWindowAccessor : IBlazorWindowAccessor
{
    private readonly SharedResourceLatch windowShortcutsSuppression = new(nameof(windowShortcutsSuppression));

    public BlazorWindowAccessor(IBlazorWindow window)
    {
        Window = window;
    }

    public IBlazorWindow Window { get; }

    public bool AreWindowShortcutsSuppressed => windowShortcutsSuppression.IsBusy;

    public IDisposable SuppressWindowShortcuts()
    {
        return windowShortcutsSuppression.Rent();
    }
}
