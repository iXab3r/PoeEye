using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using PInvoke;
using PoeShared.Logging;

namespace PoeShared.Scaffolding.WPF;

public static class WindowExtensions {
    private static readonly IFluentLog Log = typeof(WindowExtensions).PrepareLogger();

    public static IDisposable RegisterWndProc(this HwndSource hwndSource, HwndSourceHook hook)
    {
        Log.Info(() => $"Adding hook to {hwndSource}");
        hwndSource.AddHook(hook);
        return Disposable.Create(() =>
        {
            Log.Info(() => $"Removing hook from {hwndSource}");
            hwndSource.RemoveHook(hook);
        });
    }
    
    public static IDisposable RegisterWndProc(this Window instance, HwndSourceHook hook)
    {
        var hwnd = new WindowInteropHelper(instance).EnsureHandle();
        var hwndSource = HwndSource.FromHwnd(hwnd) ?? throw new ApplicationException($"Something went wrong - failed to create {nameof(HwndSource)} for handle {hwnd.ToHexadecimal()}");
        return hwndSource.RegisterWndProc(hook);
    }

    public static IDisposable LogWndProc(this Window instance, string prefix)
    {
        Log.Info(() => $"[{prefix}] Registering log hook to {instance}");
        return RegisterWndProc(instance,
            (IntPtr hwnd, int msgRaw, IntPtr param, IntPtr lParam, ref bool handled) =>
            {
                var msg = (User32.WindowMessage) msgRaw;
                if (Log.IsDebugEnabled)
                {
                    Log.Debug(() => $"[{prefix}] Message: {msg} ({msgRaw.ToHexadecimal()} = {msgRaw})");
                }
                return IntPtr.Zero;
            });
    }

    public static IntPtr GetWindowHandle(this Window instance)
    {
        if (!instance.CheckAccess())
        {
            return instance.Dispatcher.Invoke(() => GetWindowHandle(instance));
        }

        var interopHandler = new WindowInteropHelper(instance);
        return interopHandler.EnsureHandle();
    }

   
    /// <summary>
    /// Creates an observable sequence that emits a single Unit value when the Window is loaded.
    /// If the Window is already loaded when subscribing, it emits the value immediately.
    /// </summary>
    /// <param name="window">The Window to observe for loading.</param>
    /// <returns>An observable sequence that emits a single Unit value when the Window is loaded.</returns>
    public static IObservable<Unit> ListenWhenLoaded(this Window window)
    {
        return Observable.Merge(
                Observable.FromEventPattern<RoutedEventHandler, RoutedEventArgs>(
                    h => window.Loaded += h, h => window.Loaded -= h).ToUnit(),
                Observable.Return(Unit.Default).Where(_ => window.IsLoaded).ToUnit())
            .Take(1);
    }

    /// <summary>
    /// Creates an observable sequence that emits a single Unit value when the Window is rendered.
    /// </summary>
    /// <param name="window">The Window to observe for rendering.</param>
    /// <returns>An observable sequence that emits a single Unit value when the Window is rendered.</returns>
    public static IObservable<Unit> ListenWhenRendered(this Window window)
    {
        return Observable.FromEventPattern<EventHandler, EventArgs>(
                h => window.ContentRendered += h, h => window.ContentRendered -= h)
            .ToUnit();
    }

    /// <summary>
    /// Creates an observable sequence that emits a single Unit value when the Window is unloaded.
    /// </summary>
    /// <param name="window">The Window to observe for unloading.</param>
    /// <returns>An observable sequence that emits a single Unit value when the Window is unloaded.</returns>
    public static IObservable<Unit> ListenWhenUnloaded(this Window window)
    {
        return Observable.FromEventPattern<RoutedEventHandler, RoutedEventArgs>(
                h => window.Unloaded += h, h => window.Unloaded -= h)
            .ToUnit();
    }

    /// <summary>
    /// Creates an observable sequence that emits KeyEventArgs when a key is released in the Window.
    /// </summary>
    /// <param name="window">The Window to observe for key up events.</param>
    /// <returns>An observable sequence of KeyEventArgs for key up events in the Window.</returns>
    public static IObservable<KeyEventArgs> ListenWhenKeyUp(this Window window)
    {
        return Observable.FromEventPattern<KeyEventHandler, KeyEventArgs>(
                h => window.KeyUp += h, h => window.KeyUp -= h)
            .Select(x => x.EventArgs);
    }

    /// <summary>
    /// Creates an observable sequence that emits KeyEventArgs when a key is pressed in the Window.
    /// </summary>
    /// <param name="window">The Window to observe for key down events.</param>
    /// <returns>An observable sequence of KeyEventArgs for key down events in the Window.</returns>
    public static IObservable<KeyEventArgs> ListenWhenKeyDown(this Window window)
    {
        return Observable.FromEventPattern<KeyEventHandler, KeyEventArgs>(
                h => window.KeyDown += h, h => window.KeyDown -= h)
            .Select(x => x.EventArgs);
    }

    /// <summary>
    /// Creates an observable sequence that emits KeyEventArgs when a key is pressed in a preview event in the Window.
    /// </summary>
    /// <param name="window">The Window to observe for preview key down events.</param>
    /// <returns>An observable sequence of KeyEventArgs for preview key down events in the Window.</returns>
    public static IObservable<KeyEventArgs> ListenWhenPreviewKeyDown(this Window window)
    {
        return Observable.FromEventPattern<KeyEventHandler, KeyEventArgs>(
                h => window.PreviewKeyDown += h, h => window.PreviewKeyDown -= h)
            .Select(x => x.EventArgs);
    }

    /// <summary>
    /// Creates an observable sequence that emits KeyEventArgs when a key is released in a preview event in the Window.
    /// </summary>
    /// <param name="window">The Window to observe for preview key up events.</param>
    /// <returns>An observable sequence of KeyEventArgs for preview key up events in the Window.</returns>
    public static IObservable<KeyEventArgs> ListenWhenPreviewKeyUp(this Window window)
    {
        return Observable.FromEventPattern<KeyEventHandler, KeyEventArgs>(
                h => window.PreviewKeyUp += h, h => window.PreviewKeyUp -= h)
            .Select(x => x.EventArgs);
    }

    /// <summary>
    /// Creates an observable sequence that emits CancelEventArgs when the Window is closing.
    /// </summary>
    /// <param name="window">The Window to observe for closing.</param>
    /// <returns>An observable sequence of CancelEventArgs for the closing event of the Window.</returns>
    public static IObservable<CancelEventArgs> ListenWhenClosing(this Window window)
    {
        return Observable.FromEventPattern<CancelEventHandler, CancelEventArgs>(
                h => window.Closing += h, h => window.Closing -= h)
            .Select(x => x.EventArgs);
    }

    /// <summary>
    /// Creates an observable sequence that emits a single Unit value when the Window is closed.
    /// </summary>
    /// <param name="window">The Window to observe for closure.</param>
    /// <returns>An observable sequence that emits a single Unit value when the Window is closed.</returns>
    public static IObservable<Unit> ListenWhenClosed(this Window window)
    {
        return Observable.FromEventPattern<EventHandler, EventArgs>(
                h => window.Closed += h, h => window.Closed -= h)
            .ToUnit();
    }

    /// <summary>
    /// Creates an observable sequence that emits a single Unit value when the Window is activated.
    /// </summary>
    /// <param name="window">The Window to observe for activation.</param>
    /// <returns>An observable sequence that emits a single Unit value when the Window is activated.</returns>
    public static IObservable<Unit> ListenWhenActivated(this Window window)
    {
        return Observable.FromEventPattern<EventHandler, EventArgs>(
                h => window.Activated += h, h => window.Activated -= h)
            .ToUnit();
    }

    /// <summary>
    /// Creates an observable sequence that emits a single Unit value when the Window is deactivated.
    /// </summary>
    /// <param name="window">The Window to observe for deactivation.</param>
    /// <returns>An observable sequence that emits a single Unit value when the Window is deactivated.</returns>
    public static IObservable<Unit> ListenWhenDeactivated(this Window window)
    {
        return Observable.FromEventPattern<EventHandler, EventArgs>(
                h => window.Deactivated += h, h => window.Deactivated -= h)
            .ToUnit();
    }

    private static IObservable<Unit> ToUnit<TEventArgs>(
        this IObservable<EventPattern<TEventArgs>> source) where TEventArgs : EventArgs
    {
        return source.Select(_ => Unit.Default);
    }
}