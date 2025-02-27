using System;
using PoeShared.UI;

namespace PoeShared.Blazor.Wpf;

/// <summary>
/// Defines a contract for managing native window properties and operations within the Blazor environment,
/// providing direct control over window positioning, sizing, and retrieval of window handles.
/// </summary>
public interface IBlazorWindowNativeController
{
    /// <summary>
    /// Retrieves the handle (HWND) of the native window associated with this Blazor window instance.
    /// </summary>
    /// <returns>An <see cref="IntPtr"/> representing the native window handle.</returns>
    IntPtr GetWindowHandle();

    /// <summary>
    /// Gets the rectangular bounds of the native window, including position and size.
    /// </summary>
    /// <returns></returns>
    System.Drawing.Rectangle GetWindowRect();
    
    /// <summary>
    /// Sets the rectangular bounds of the native window, including position and size.
    /// </summary>
    /// <param name="windowRect">A <see cref="System.Drawing.Rectangle"/> specifying the new position and size of the window.</param>
    void SetWindowRect(System.Drawing.Rectangle windowRect);

    /// <summary>
    /// Sets the size dimensions of the native window without altering its current position.
    /// </summary>
    /// <param name="windowSize">A <see cref="System.Drawing.Size"/> specifying the new width and height of the window.</param>
    void SetWindowSize(System.Drawing.Size windowSize);

    /// <summary>
    /// Updates the position of the native window without changing its size.
    /// </summary>
    /// <param name="windowPos">A <see cref="System.Drawing.Point"/> specifying the new top-left corner position of the window.</param>
    void SetWindowPos(System.Drawing.Point windowPos);
}

public interface IBlazorWindowMetroController
{
    ReactiveMetroWindowBase GetWindow();

    void EnsureCreated();
}