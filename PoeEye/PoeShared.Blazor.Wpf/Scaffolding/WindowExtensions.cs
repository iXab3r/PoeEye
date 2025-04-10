using System;
using System.Reactive.Disposables;
using System.Windows.Interop;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Wpf.Scaffolding;

internal static class WindowExtensions
{
    private static readonly IFluentLog Log = typeof(WindowExtensions).PrepareLogger();

    public static IDisposable RegisterWndProc(this HwndSource hwndSource, HwndSourceHook hook)
    {
        Log.Info($"Adding hook to {hwndSource}");
        hwndSource.AddHook(hook);
        return Disposable.Create(() =>
        {
            Log.Info($"Removing hook from {hwndSource}");
            hwndSource.RemoveHook(hook);
        });
    }
}