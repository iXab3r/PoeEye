using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PoeShared.Blazor.Scaffolding;
using PoeShared.Blazor.Services;

namespace PoeShared.Blazor.Wpf;

/// <summary>
/// Bridges DOM title bar gestures to the native window operations shared by Blazor window hosts.
/// </summary>
internal sealed class BlazorWindowTitleBarGestureHandler : IAsyncDisposable
{
    private readonly IBlazorWindow window;
    private readonly DotNetObjectReference<BlazorWindowTitleBarGestureHandler> dotNetObjectReference;
    private JsWindowTitleBarGestureRef? registration;
    private bool isDisposed;

    private BlazorWindowTitleBarGestureHandler(IBlazorWindow window)
    {
        this.window = window ?? throw new ArgumentNullException(nameof(window));
        dotNetObjectReference = DotNetObjectReference.Create(this);
    }

    public static async Task<BlazorWindowTitleBarGestureHandler?> TryRegisterAsync(
        IBlazorWindow window,
        IJsPoeBlazorUtils jsPoeBlazorUtils,
        ElementReference titleBarElement,
        double? dragThresholdPixels = null)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(jsPoeBlazorUtils);

        var handler = new BlazorWindowTitleBarGestureHandler(window);
        try
        {
            handler.registration = await jsPoeBlazorUtils.RegisterWindowTitleBarGestures(
                titleBarElement,
                handler.dotNetObjectReference,
                dragThresholdPixels ?? GetDefaultDragThresholdPixels());
            return handler;
        }
        catch (Exception e) when (e.IsJSException() || e is ObjectDisposedException)
        {
            window.Log.Warn("Failed to register Blazor window titlebar gestures", e);
            await handler.DisposeJsSafeAsync();
            return null;
        }
    }

    [JSInvokable]
    public void HandleTitleBarDragStart()
    {
        if (isDisposed)
        {
            return;
        }

        window.EnableDragMove();
    }

    [JSInvokable]
    public void HandleTitleBarDoubleClick()
    {
        if (isDisposed || !CanToggleWindowState())
        {
            return;
        }

        if (window.WindowState == WindowState.Normal)
        {
            window.Maximize();
        }
        else if (window.WindowState == WindowState.Maximized)
        {
            window.Restore();
        }
    }

    [JSInvokable]
    public void HandleTitleBarContextMenu()
    {
        if (isDisposed)
        {
            return;
        }

        try
        {
            UnsafeNative.ShowSystemMenuAt(window.GetWindowHandle(), UnsafeNative.GetCursorPosition());
        }
        catch (Exception e)
        {
            window.Log.Warn("Failed to show Blazor window system menu from titlebar", e);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (isDisposed)
        {
            return;
        }

        isDisposed = true;
        if (registration is { } registrationRef)
        {
            await registrationRef.DisposeJsSafeAsync();
        }

        dotNetObjectReference.DisposeJsSafe();
    }

    private static double GetDefaultDragThresholdPixels()
    {
        return Math.Max(
            SystemParameters.MinimumHorizontalDragDistance,
            SystemParameters.MinimumVerticalDragDistance);
    }

    private bool CanToggleWindowState()
    {
        return window.ShowMaxButton
               && window.ResizeMode is ResizeMode.CanResize or ResizeMode.CanResizeWithGrip;
    }
}
