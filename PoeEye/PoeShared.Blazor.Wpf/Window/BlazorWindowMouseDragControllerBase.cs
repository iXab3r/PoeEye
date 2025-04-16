using System;
using System.Drawing;
using System.Reactive.Disposables;
using System.Windows.Forms;
using System.Windows.Input;
using PoeShared.Scaffolding;
using Cursor = System.Windows.Input.Cursor;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace PoeShared.Blazor.Wpf;

/// <summary>
/// Base class for implementing mouse-based window dragging behavior in a Blazor-integrated window.
/// Manages mouse capture, drag threshold detection, and cursor changes during drag operations.
/// </summary>
public abstract class BlazorWindowMouseDragControllerBase : DisposableReactiveObject
{
    private bool changedCursor;
    private bool draggingStarted;
    private bool changedCursorBack;
    private Cursor originalCursor;
    private Point dragStartPoint;

    public BlazorWindowMouseDragControllerBase(IBlazorWindow blazorWindow, BlazorContentControl contentControl)
    {
        BlazorWindow = blazorWindow;
        ContentControl = contentControl;

        StartPoint = GetCursorPosition();
        WindowInitialPosition = new Point(blazorWindow.Left, blazorWindow.Top);
        WindowInitialSize = new Size(blazorWindow.Width, blazorWindow.Height);

        if (!ContentControl.CaptureMouse())
        {
            throw new ApplicationException($"Failed to capture mouse inside window {blazorWindow}");
        }

        originalCursor = Mouse.OverrideCursor;
        ContentControl.MouseUp += ControlOnMouseUp;
        ContentControl.MouseMove += ControlOnMouseMove;

        Disposable.Create(() =>
        {
            ContentControl.MouseUp -= ControlOnMouseUp;
            ContentControl.MouseMove -= ControlOnMouseMove;

            if (changedCursorBack == false)
            {
                changedCursorBack = true;
                Mouse.OverrideCursor = originalCursor;
            }

            try
            {
                ContentControl.ReleaseMouseCapture();
            }
            catch (Exception)
            {
                //not critical at this point
            }
        }).AddTo(Anchors);
    }

    /// <summary>
    /// Gets the associated Blazor window being dragged.
    /// </summary>
    public IBlazorWindow BlazorWindow { get; }

    /// <summary>
    /// Gets the content control where mouse interaction is captured.
    /// </summary>
    public BlazorContentControl ContentControl { get; }

    /// <summary>
    ///  Gets the dimensions in pixels, of the rectangle that a drag operation must
    ///  extend to be considered a drag. The rectangle is centered on a drag point.
    /// </summary>
    public Size DragSize { get; init; }

    /// <summary>
    /// Gets the original window position when the drag began.
    /// </summary>
    public Point WindowInitialPosition { get; }

    /// <summary>
    /// Gets the original window size when the drag began.
    /// </summary>
    public Size WindowInitialSize { get; }

    /// <summary>
    /// Gets the cursor position when the drag was initiated.
    /// </summary>
    public Point StartPoint { get; }

    /// <summary>
    /// Gets a value indicating whether the drag operation is still active.
    /// </summary>
    public bool IsDragging => !Anchors.IsDisposed && draggingStarted;

    /// <summary>
    /// Gets the current screen coordinates of the mouse cursor.
    /// </summary>
    public Point CursorPosition => GetCursorPosition();

    /// <summary>
    /// Called during a drag operation when the mouse is moved.
    /// Subclasses must implement logic for repositioning/resizing the window.
    /// </summary>
    /// <param name="cursorPosition">The current mouse cursor position.</param>
    protected virtual void HandleMove(Point cursorPosition)
    {
        var diffX = cursorPosition.X - dragStartPoint.X;
        var diffY = cursorPosition.Y - dragStartPoint.Y;
        HandleMove(diffX, diffY);
    }
    
    /// <summary>
    /// Called during a drag operation when the mouse is moved, delta takes into consideration start point, cursor position, etc
    /// </summary>
    protected abstract void HandleMove(int deltaX, int deltaY);

    /// <summary>
    /// Optionally overrides the cursor shown during dragging.
    /// </summary>
    /// <returns>A custom cursor to use during dragging, or <c>null</c> to use the default.</returns>
    protected virtual Cursor GetOverrideCursor()
    {
        return null;
    }

    private void ControlOnMouseMove(object sender, MouseEventArgs e)
    {
        var current = GetCursorPosition();

        switch (draggingStarted)
        {
            case false when Math.Abs(current.X - StartPoint.X) < DragSize.Width && Math.Abs(current.Y - StartPoint.Y) < DragSize.Height:
                return;
            case false:
                draggingStarted = true;
                dragStartPoint = current;
                break;
        }

        if (changedCursor == false)
        {
            changedCursor = true;
            var overrideCursor = GetOverrideCursor();
            if (overrideCursor != null)
            {
                originalCursor = Mouse.OverrideCursor;
                Mouse.OverrideCursor = overrideCursor;
            }
            else
            {
                changedCursorBack = true;
            }
        }

        HandleMove(current);
    }

    private void ControlOnMouseUp(object sender, MouseButtonEventArgs e)
    {
        Dispose();
    }

    private static Point GetCursorPosition()
    {
        return System.Windows.Forms.Cursor.Position;
    }
}