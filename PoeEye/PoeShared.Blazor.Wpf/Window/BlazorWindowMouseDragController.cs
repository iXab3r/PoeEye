using System.Drawing;
using System.Reactive.Disposables;
using System.Windows.Forms;
using PoeShared.Scaffolding;
using Cursor = System.Windows.Input.Cursor;
using Cursors = System.Windows.Input.Cursors;

namespace PoeShared.Blazor.Wpf;

/// <summary>
/// Implements the default window drag behavior for a <see cref="IBlazorWindow"/>, enabling the window
/// to be repositioned via mouse dragging
/// </summary>
public class BlazorWindowMouseDragController : BlazorWindowMouseDragControllerBase
{
    public BlazorWindowMouseDragController(
        IBlazorWindow blazorWindow, 
        BlazorContentControl contentControl) : base(blazorWindow, contentControl)
    {
        DragSize = SystemInformation.DragSize;
        blazorWindow.Log.Debug($"Window dragging has been started: {new { StartPoint, WindowInitialPosition, DragSize }}");
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
                DragSize
            }}");
        }).AddTo(Anchors);
    }

    protected override Cursor GetOverrideCursor()
    {
        return Cursors.SizeAll;
    }

    protected override void HandleMove(int deltaX, int deltaY)
    {
        BlazorWindow.SetWindowPos(new Point(WindowInitialPosition.X + deltaX, WindowInitialPosition.Y + deltaY));
    }
}