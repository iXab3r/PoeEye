using System;
using System.Drawing;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using JetBrains.Annotations;
using PInvoke;

namespace PoeShared.Native;

/// <summary>
/// Provides a handle to a window.
/// </summary>
public interface IWindowHandle : IWin32Window, IDisposable, IEquatable<IWindowHandle>
{
    /// <summary>
    /// Gets the title of the window.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Gets the outer dimensions of the window, including any title bar and border.
    /// </summary>
    [Obsolete("In most cases DwmFrameBounds is much-much more reliable as WindowRect will include invisible borders/decorations/etc")]
    WinRect WindowRect { get; }

    /// <summary>
    /// Gets the dimensions of the client area of the window.
    /// </summary>
    [Obsolete("Not very reliable as will be affected by system settings")]
    WinRect ClientRect { get; }
    
    /// <summary>
    /// Gets the extended window frame bounds provided by DWM.
    /// </summary>
    WinRect DwmFrameBounds { get; }
    
    /// <summary>
    /// Gets the DWM window frame bounds within monitor.
    /// </summary>
    WinRect DwmFrameBoundsWithinMonitor { get; }
    
    /// <summary>
    /// Gets window title bar bounds
    /// </summary>
    WinRect TitleBarBounds { get; }
    
    RECT AdjustWindowRectForDpi { get; }

    /// <summary>
    /// Gets the icon of the window.
    /// </summary>
    Icon Icon { get; }

    /// <summary>
    /// Gets the icon of the window as a BitmapSource.
    /// </summary>
    BitmapSource IconBitmap { get; }

    /// <summary>
    /// Gets the class name of the window.
    /// </summary>
    string Class { get; }

    /// <summary>
    /// Gets the process identifier associated with the window.
    /// </summary>
    int ProcessId { get; }

    /// <summary>
    /// Gets the thread identifier that created the window.
    /// </summary>
    int ThreadId { get; }

    /// <summary>
    /// Gets the parent process identifier of the process that created the window.
    /// </summary>
    int ParentProcessId { get; }

    /// <summary>
    /// Gets the creation time of the process that created the window.
    /// </summary>
    DateTime CreatedAt { get; }

    /// <summary>
    /// Gets the full path of the executable file of the process that created the window.
    /// </summary>
    string ProcessPath { get; }

    /// <summary>
    /// Gets the name of the process that created the window.
    /// </summary>
    string ProcessName { get; }

    /// <summary>
    /// Gets the arguments of the process that created the window.
    /// </summary>
    string ProcessArgs { get; }

    /// <summary>
    /// Gets the command line of the process that created the window.
    /// </summary>
    string CommandLine { get; }

    /// <summary>
    /// Gets or sets the z-order of the window. 
    /// </summary>
    int ZOrder { get; set; }

    /// <summary>
    /// Gets the window styles.
    /// </summary>
    User32.WindowStyles WindowStyle { get; }

    /// <summary>
    /// Gets the extended window styles.
    /// </summary>
    User32.WindowStylesEx WindowStylesEx { get; }

    /// <summary>
    /// Gets a value indicating whether the window is visible.
    /// </summary>
    bool IsVisible { get; }

    /// <summary>
    /// Gets a value indicating whether the window is minimized (iconic).
    /// </summary>
    bool IsIconic { get; }

    /// <summary>
    /// Gets the owner of the window.
    /// </summary>
    IWindowHandle Owner { get; }

    /// <summary>
    /// Gets the parent of the window.
    /// </summary>
    IWindowHandle Parent { get; }
}
