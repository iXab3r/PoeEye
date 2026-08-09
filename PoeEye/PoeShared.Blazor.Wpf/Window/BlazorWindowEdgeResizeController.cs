using System.Drawing;
using System.Windows.Forms;

namespace PoeShared.Blazor.Wpf;

/// <summary>
/// Implements the default window resize behavior for a <see cref="INativeWindow"/>, enabling the window
/// to be resized by dragging edges/corners.
/// <para>
/// The new size is constrained to the aspect ratio the window had when the drag started while SHIFT is held,
/// or whenever the window exposes an enabled <see cref="IWindowAspectRatioController"/>. Corner drags scale
/// proportionally and edge drags derive the other dimension; releasing SHIFT mid-drag returns to free resizing
/// when no window-level constraint is enabled. Min/Max window size limits are always honored,
/// and the edges opposite to the dragged ones stay anchored (see <see cref="WindowResizeMath"/>).
/// </para>
/// </summary>
public class BlazorWindowEdgeResizeController : BlazorWindowMouseDragControllerBase
{
    private readonly WindowResizeDirection direction;

    public BlazorWindowEdgeResizeController(
        INativeWindow blazorWindow,
        System.Windows.UIElement contentControl,
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
        var initialBounds = new Rectangle(
            WindowInitialPosition.X,
            WindowInitialPosition.Y,
            WindowInitialSize.Width,
            WindowInitialSize.Height
        );

        var keepAspectRatio = BlazorWindow is IWindowAspectRatioController { TargetAspectRatio: > 0 }
                              || (Control.ModifierKeys & Keys.Shift) == Keys.Shift;
        var rect = WindowResizeMath.CalculateBounds(
            initialBounds,
            direction,
            deltaX,
            deltaY,
            keepAspectRatio,
            minSize: new Size(BlazorWindow.MinWidth, BlazorWindow.MinHeight),
            maxSize: new Size(BlazorWindow.MaxWidth, BlazorWindow.MaxHeight));
        BlazorWindow.SetWindowRect(rect);
    }
}
