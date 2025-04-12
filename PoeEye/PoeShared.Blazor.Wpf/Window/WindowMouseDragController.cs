using System.Drawing;
using System.Reactive.Disposables;
using System.Windows.Input;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Wpf;

internal sealed class WindowMouseDragController : WindowMouseDragControllerBase
{
    public WindowMouseDragController(IBlazorWindow blazorWindow, BlazorContentControl contentControl) : base(blazorWindow, contentControl)
    {
        blazorWindow.Log.Debug($"Window dragging has been started: {new { StartPoint, WindowInitialPosition }}");

        Disposable.Create(() =>
        {
            blazorWindow.Log.Debug($"Window dragging has stopped: {new
            {
                StartPoint,
                EndPoint = CursorPosition,
                WindowInitialPosition,
                WindowInitialSize,
                WindowPosition = new Point(blazorWindow.Left, blazorWindow.Top),
                WindowSize = new Point(blazorWindow.Width, blazorWindow.Height),
            }}");
        }).AddTo(Anchors);
    }

    protected override Cursor GetOverrideCursor()
    {
        return Cursors.SizeAll;
    }

    protected override void HandleMove(Point cursorPosition)
    {
        var diffX = cursorPosition.X - StartPoint.X;
        var diffY = cursorPosition.Y - StartPoint.Y;
        BlazorWindow.SetWindowPos(new Point(WindowInitialPosition.X + diffX, WindowInitialPosition.Y + diffY));
    }
}

internal enum WindowResizeDirection
{
    None,
    Left,
    Top,
    Right,
    Bottom,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}