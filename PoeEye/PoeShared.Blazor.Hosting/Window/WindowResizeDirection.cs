namespace PoeShared.Blazor.Wpf;

/// <summary>
/// Specifies the direction(s) in which a window can be resized.
/// Typically used to determine the cursor type or the drag behavior during edge/corner resizing.
/// </summary>
public enum WindowResizeDirection
{
    /// <summary>
    /// No resize operation is in progress or allowed.
    /// </summary>
    None,

    /// <summary>
    /// Resizing from the left edge of the window.
    /// </summary>
    Left,

    /// <summary>
    /// Resizing from the top edge of the window.
    /// </summary>
    Top,

    /// <summary>
    /// Resizing from the right edge of the window.
    /// </summary>
    Right,

    /// <summary>
    /// Resizing from the bottom edge of the window.
    /// </summary>
    Bottom,

    /// <summary>
    /// Resizing from the top-left corner of the window.
    /// </summary>
    TopLeft,

    /// <summary>
    /// Resizing from the top-right corner of the window.
    /// </summary>
    TopRight,

    /// <summary>
    /// Resizing from the bottom-left corner of the window.
    /// </summary>
    BottomLeft,

    /// <summary>
    /// Resizing from the bottom-right corner of the window.
    /// </summary>
    BottomRight
}