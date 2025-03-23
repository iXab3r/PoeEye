using System;
using System.Drawing;
using PoeShared.Logging;
using PoeShared.Native;

namespace PoeShared.Scaffolding;

public static class WindowHandleExtensions
{
    /// <summary>
    /// Activates the specified window by bringing it to the foreground.
    /// </summary>
    /// <param name="window">The window handle to activate.</param>
    /// <exception cref="InvalidStateException">
    /// Thrown if the window fails to become the foreground window within the specified timeout.
    /// </exception>
    public static void Activate(this IWindowHandle window)
    {
        UnsafeNative.ActivateWindow(window);
    }

    /// <summary>
    /// Activates the specified window by bringing it to the foreground.
    /// </summary>
    /// <param name="window">The window handle to activate.</param>
    /// <param name="timeout">The maximum time allowed for the activation attempt.</param>
    /// <exception cref="InvalidStateException">
    /// Thrown if the window fails to become the foreground window within the specified timeout.
    /// </exception>
    public static void Activate(this IWindowHandle window, TimeSpan timeout)
    {
        UnsafeNative.ActivateWindow(window, timeout);
    }

    /// <summary>
    /// Activates the specified window by bringing it to the foreground.
    /// </summary>
    /// <param name="window">The window handle to activate.</param>
    /// <param name="timeout">The maximum time allowed for the activation attempt.</param>
    /// <param name="log">The logger for capturing diagnostic information.</param>
    /// <exception cref="InvalidStateException">
    /// Thrown if the window fails to become the foreground window within the specified timeout.
    /// </exception>
    public static void Activate(this IWindowHandle window, TimeSpan timeout, IFluentLog log)
    {
        UnsafeNative.ActivateWindow(window, timeout, log);
    }

    /// <summary>
    /// Converts a point in the window's coordinate space to the corresponding screen coordinate.
    /// </summary>
    /// <param name="window">The window handle providing the coordinate context.</param>
    /// <param name="windowPoint">The point in window coordinates to convert.</param>
    /// <returns>The corresponding screen coordinate.</returns>
    /// <exception cref="InvalidStateException">
    /// Thrown if the target window is not found or has invalid bounds.
    /// </exception>
    public static WinPoint ToScreen(this IWindowHandle window, Point windowPoint)
    {
        var windowBounds = window.DwmFrameBounds;
        if (!windowBounds.IsNotEmptyArea())
        {
            throw new InvalidStateException($"Something went wrong - target window {window} is not found or does not have valid window bounds");
        }

        return new WinPoint(windowPoint.X + windowBounds.Left, windowPoint.Y + windowBounds.Top);
    }

    /// <summary>
    /// Converts a point in screen coordinates to the corresponding window coordinate.
    /// </summary>
    /// <param name="window">The window handle providing the coordinate context.</param>
    /// <param name="screenPoint">The point in screen coordinates to convert.</param>
    /// <returns>The corresponding window coordinate.</returns>
    /// <exception cref="InvalidStateException">
    /// Thrown if the target window is not found or has invalid bounds.
    /// </exception>
    public static WinPoint FromScreen(this IWindowHandle window, Point screenPoint)
    {
        var windowBounds = window.DwmFrameBounds;
        if (!windowBounds.IsNotEmptyArea())
        {
            throw new InvalidStateException($"Something went wrong - target window {window} is not found or does not have valid window bounds");
        }

        return new WinPoint(screenPoint.X - windowBounds.Left, screenPoint.Y - windowBounds.Top);
    }
}