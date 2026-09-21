using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Disposables;

namespace PoeShared.Blazor.Wpf;

/// <summary>
/// Process-local discovery of window controllers. Registration does not create an HWND or keep a window alive.
/// Window state and native operations remain owned by each controller's dispatcher.
/// </summary>
internal sealed class NativeWindowRegistry
{
    internal static NativeWindowRegistry Instance { get; } = new();
    private readonly ConcurrentDictionary<long, WeakReference<NativeWindow>> windows = new();
    private WeakReference<NativeWindow> mainWindow;

    internal IDisposable Register(NativeWindow window)
    {
        var id = window.RegistryId;
        windows[id] = new WeakReference<NativeWindow>(window);
        return Disposable.Create(() => windows.TryRemove(id, out _));
    }

    internal IReadOnlyList<NativeWindow> GetWindows()
    {
        var result = new List<NativeWindow>();
        foreach (var pair in windows)
        {
            if (pair.Value.TryGetTarget(out var window) && !window.RegistryClosed)
                result.Add(window);
            else
                windows.TryRemove(pair.Key, out _);
        }
        return result;
    }

    internal bool TryGetWindow(IntPtr handle, out NativeWindow window)
    {
        if (handle != IntPtr.Zero)
            foreach (var candidate in GetWindows())
                if (candidate.RegistryHandle == handle)
                {
                    window = candidate;
                    return true;
                }
        window = null;
        return false;
    }

    internal void SetMainWindow(NativeWindow window) => mainWindow = new WeakReference<NativeWindow>(window);

    internal NativeWindow GetMainWindow() =>
        mainWindow != null && mainWindow.TryGetTarget(out var window) && !window.RegistryClosed ? window : null;
}
