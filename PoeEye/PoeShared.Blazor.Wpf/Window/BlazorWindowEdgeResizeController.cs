using System;
using System.Drawing;
using System.Windows.Forms;

namespace PoeShared.Blazor.Wpf;

/// <summary>
/// Implements the default window resize behavior for a <see cref="IBlazorWindow"/>, enabling the window
/// to be resized by dragging edges/corners
/// </summary>
public class BlazorWindowEdgeResizeController : BlazorWindowMouseDragControllerBase
{
    private readonly WindowResizeDirection direction;

    public BlazorWindowEdgeResizeController(
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

    protected override void HandleMove(int deltaX, int deltaY)
    {
        var dx = deltaX;
        var dy = deltaY;
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