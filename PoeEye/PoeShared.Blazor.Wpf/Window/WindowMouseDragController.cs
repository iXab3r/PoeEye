using System.Drawing;

namespace PoeShared.Blazor.Wpf;

internal sealed class WindowMouseDragController : WindowMouseDragControllerBase
{
    public WindowMouseDragController(IBlazorWindow blazorWindow, BlazorContentControl contentControl) : base(blazorWindow, contentControl)
    {
        
    }

    protected override void HandleMove(Point cursorPosition)
    {
        var diffX = cursorPosition.X - StartPoint.X;
        var diffY = cursorPosition.Y - StartPoint.Y;
        BlazorWindow.SetWindowPos(new Point(WindowInitialPosition.X + diffX, WindowInitialPosition.Y + diffY));
    }
}