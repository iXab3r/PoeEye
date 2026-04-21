using System;

namespace PoeShared.Native;

/// <summary>
/// Creates PoeShared handle wrappers for raw Win32 window and monitor handles.
/// </summary>
/// <remarks>
/// The provider is the infrastructure boundary between APIs that expose raw
/// <see cref="IntPtr"/> values, such as <c>HWND</c> or <c>HMONITOR</c>, and code
/// that expects richer PoeShared abstractions. The returned wrappers keep the
/// original native handle and expose metadata through <see cref="IWindowHandle"/>
/// or <see cref="IMonitorHandle"/>.
///
/// This service does not enumerate, validate, or own native windows. A wrapper
/// can represent a stale or invalid handle; consumers that care about liveness
/// should check <see cref="IWindowHandle.IsWindow"/> or use a window-list service
/// that tracks current windows.
/// </remarks>
public interface IWindowHandleProvider
{
    /// <summary>
    /// Creates an <see cref="IWindowHandle"/> wrapper for a raw Win32
    /// <c>HWND</c>.
    /// </summary>
    /// <param name="hwnd">The raw window handle to wrap.</param>
    /// <returns>A wrapper over <paramref name="hwnd"/>.</returns>
    IWindowHandle GetByWindowHandle(IntPtr hwnd);

    /// <summary>
    /// Creates an <see cref="IMonitorHandle"/> wrapper for a raw Win32
    /// <c>HMONITOR</c>.
    /// </summary>
    /// <param name="hMonitor">The raw monitor handle to wrap.</param>
    /// <returns>A wrapper over <paramref name="hMonitor"/>.</returns>
    IMonitorHandle GetByMonitorHandle(IntPtr hMonitor);

    /// <summary>
    /// Resolves the monitor nearest to a raw window handle and wraps it as an
    /// <see cref="IMonitorHandle"/>.
    /// </summary>
    /// <param name="hwnd">The raw window handle used as monitor lookup input.</param>
    /// <returns>The nearest monitor wrapper returned by the platform lookup.</returns>
    IMonitorHandle GetMonitorByWindowHandle(IntPtr hwnd);
}
