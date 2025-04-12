using System;
using System.Drawing;
using System.Windows.Input;

namespace PoeShared.Blazor.Wpf;

internal sealed class WindowEdgeResizeController : WindowMouseDragControllerBase
{
    private readonly WindowResizeDirection direction;

    public WindowEdgeResizeController(
        IBlazorWindow blazorWindow,
        BlazorContentControl contentControl,
        WindowResizeDirection direction)
        : base(blazorWindow, contentControl)
    {
        this.direction = direction;
        blazorWindow.Log.Debug($"Window resize has been started: {new { StartPoint, WindowInitialPosition, WindowInitialSize, direction }}");
    }

    protected override Cursor GetOverrideCursor()
    {
        return direction switch
        {
            WindowResizeDirection.Top or WindowResizeDirection.Bottom => Cursors.SizeNS,
            WindowResizeDirection.Left or WindowResizeDirection.Right => Cursors.SizeWE,
            WindowResizeDirection.TopLeft or WindowResizeDirection.BottomRight => Cursors.SizeNWSE,
            WindowResizeDirection.TopRight or WindowResizeDirection.BottomLeft => Cursors.SizeNESW,
            _ => null
        };
    }

    protected override void HandleMove(Point cursorPosition)
    {
        var dx = cursorPosition.X - StartPoint.X;
        var dy = cursorPosition.Y - StartPoint.Y;

        var rect = new Rectangle(
            WindowInitialPosition.X,
            WindowInitialPosition.Y,
            WindowInitialSize.Width,
            WindowInitialSize.Height
        );

        switch (direction)
        {
            case WindowResizeDirection.Left:
                rect.X += dx;
                rect.Width -= dx;
                break;
            case WindowResizeDirection.Right:
                rect.Width += dx;
                break;
            case WindowResizeDirection.Top:
                rect.Y += dy;
                rect.Height -= dy;
                break;
            case WindowResizeDirection.Bottom:
                rect.Height += dy;
                break;
            case WindowResizeDirection.TopLeft:
                rect.X += dx;
                rect.Width -= dx;
                rect.Y += dy;
                rect.Height -= dy;
                break;
            case WindowResizeDirection.TopRight:
                rect.Width += dx;
                rect.Y += dy;
                rect.Height -= dy;
                break;
            case WindowResizeDirection.BottomLeft:
                rect.X += dx;
                rect.Width -= dx;
                rect.Height += dy;
                break;
            case WindowResizeDirection.BottomRight:
                rect.Width += dx;
                rect.Height += dy;
                break;
        }

        // Prevent collapsing to negative or zero size
        rect.Width = Math.Max(BlazorWindow.MinWidth, rect.Width);
        rect.Height = Math.Max(BlazorWindow.MinHeight, rect.Height);

        BlazorWindow.SetWindowRect(rect);
    }
}