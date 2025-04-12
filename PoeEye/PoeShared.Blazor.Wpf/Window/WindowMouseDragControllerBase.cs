using System;
using System.Drawing;
using System.Reactive.Disposables;
using System.Windows.Input;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Wpf;

internal abstract class WindowMouseDragControllerBase : DisposableReactiveObject
{
    private readonly AtomicFlag changedCursor = new AtomicFlag();
    private readonly AtomicFlag changedCursorBack = new AtomicFlag();
    private Cursor originalCursor;

    public WindowMouseDragControllerBase(IBlazorWindow blazorWindow, BlazorContentControl contentControl)
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

            if (changedCursorBack.Set())
            {
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

    public IBlazorWindow BlazorWindow { get; }

    public BlazorContentControl ContentControl { get; }

    public Point WindowInitialPosition { get; }

    public Size WindowInitialSize { get; }

    public Point StartPoint { get; }

    public bool IsDragging => !Anchors.IsDisposed;

    public Point CursorPosition => GetCursorPosition();

    protected abstract void HandleMove(Point cursorPosition);

    protected virtual Cursor GetOverrideCursor()
    {
        return null;
    }

    private void ControlOnMouseMove(object sender, MouseEventArgs e)
    {
        if (changedCursor.Set())
        {
            var overrideCursor = GetOverrideCursor();
            if (overrideCursor != null)
            {
                originalCursor = Mouse.OverrideCursor;
                Mouse.OverrideCursor = overrideCursor;
            }
            else
            {
                changedCursorBack.Set();
            }
        }

        var current = GetCursorPosition();
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