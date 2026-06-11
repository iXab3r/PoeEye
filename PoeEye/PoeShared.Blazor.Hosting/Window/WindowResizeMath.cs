using System;
using System.Drawing;

namespace PoeShared.Blazor.Wpf;

/// <summary>
/// Pure geometry for interactive edge/corner window resizing - shared by all window stacks which implement
/// the resize drag loop in managed code (e.g. WPF <c>BlazorWindowEdgeResizeController</c>).
/// All coordinates are physical pixels, deltas are relative to the drag start point.
/// </summary>
public static class WindowResizeMath
{
    /// <summary>
    /// Calculates the new window bounds for a resize drag.
    /// <list type="number">
    /// <item>The edges referenced by <paramref name="direction"/> are moved by the deltas.</item>
    /// <item>When <paramref name="keepAspectRatio"/> is set, the size is constrained to the aspect ratio of
    /// <paramref name="initialBounds"/>: corner drags scale proportionally along the dominant axis, edge drags
    /// derive the other dimension from the dragged one.</item>
    /// <item>The size is clamped to <paramref name="minSize"/>/<paramref name="maxSize"/> (components &lt;= 0 mean "no limit";
    /// size never collapses below 1x1).</item>
    /// <item>The edges NOT being dragged are anchored: e.g. dragging the left edge keeps the right edge in place
    /// even when the size gets clamped or aspect-corrected.</item>
    /// </list>
    /// </summary>
    public static Rectangle CalculateBounds(
        Rectangle initialBounds,
        WindowResizeDirection direction,
        int deltaX,
        int deltaY,
        bool keepAspectRatio = false,
        Size minSize = default,
        Size maxSize = default)
    {
        var size = CalculateSize(initialBounds.Size, direction, deltaX, deltaY);
        if (keepAspectRatio)
        {
            size = ConstrainAspectRatio(size, initialBounds.Size, direction);
        }

        size = ClampSize(size, minSize, maxSize);
        return AnchorOppositeEdges(initialBounds, direction, size);
    }

    /// <summary>
    /// Moves the edges referenced by <paramref name="direction"/> by the given deltas and returns the resulting size.
    /// </summary>
    public static Size CalculateSize(Size initialSize, WindowResizeDirection direction, int deltaX, int deltaY)
    {
        var width = initialSize.Width;
        var height = initialSize.Height;

        switch (direction)
        {
            case WindowResizeDirection.Left:
                width -= deltaX;
                break;
            case WindowResizeDirection.Right:
                width += deltaX;
                break;
            case WindowResizeDirection.Top:
                height -= deltaY;
                break;
            case WindowResizeDirection.Bottom:
                height += deltaY;
                break;
            case WindowResizeDirection.TopLeft:
                width -= deltaX;
                height -= deltaY;
                break;
            case WindowResizeDirection.TopRight:
                width += deltaX;
                height -= deltaY;
                break;
            case WindowResizeDirection.BottomLeft:
                width -= deltaX;
                height += deltaY;
                break;
            case WindowResizeDirection.BottomRight:
                width += deltaX;
                height += deltaY;
                break;
        }

        return new Size(width, height);
    }

    /// <summary>
    /// Constrains <paramref name="size"/> to the aspect ratio of <paramref name="initialSize"/>:
    /// for edge drags the dragged dimension wins and the other one is derived from it,
    /// for corner drags both dimensions are scaled proportionally along the dominant (most-changed) axis.
    /// </summary>
    public static Size ConstrainAspectRatio(Size size, Size initialSize, WindowResizeDirection direction)
    {
        if (initialSize.Width <= 0 || initialSize.Height <= 0)
        {
            return size;
        }

        var aspectRatio = initialSize.Width / (double) initialSize.Height;
        switch (direction)
        {
            case WindowResizeDirection.Left:
            case WindowResizeDirection.Right:
            {
                var width = Math.Max(1, size.Width);
                return new Size(width, Math.Max(1, (int) Math.Round(width / aspectRatio)));
            }
            case WindowResizeDirection.Top:
            case WindowResizeDirection.Bottom:
            {
                var height = Math.Max(1, size.Height);
                return new Size(Math.Max(1, (int) Math.Round(height * aspectRatio)), height);
            }
            case WindowResizeDirection.TopLeft:
            case WindowResizeDirection.TopRight:
            case WindowResizeDirection.BottomLeft:
            case WindowResizeDirection.BottomRight:
            {
                var scaleX = size.Width / (double) initialSize.Width;
                var scaleY = size.Height / (double) initialSize.Height;
                var scale = Math.Abs(scaleX - 1) >= Math.Abs(scaleY - 1) ? scaleX : scaleY;
                return new Size(
                    Math.Max(1, (int) Math.Round(initialSize.Width * scale)),
                    Math.Max(1, (int) Math.Round(initialSize.Height * scale)));
            }
            default:
                return size;
        }
    }

    /// <summary>
    /// Clamps the size to the provided limits - components &lt;= 0 mean "no limit". The size never collapses below 1x1.
    /// </summary>
    public static Size ClampSize(Size size, Size minSize, Size maxSize)
    {
        var width = Math.Max(size.Width, Math.Max(1, minSize.Width));
        var height = Math.Max(size.Height, Math.Max(1, minSize.Height));
        if (maxSize.Width > 0)
        {
            width = Math.Min(width, maxSize.Width);
        }

        if (maxSize.Height > 0)
        {
            height = Math.Min(height, maxSize.Height);
        }

        return new Size(width, height);
    }

    /// <summary>
    /// Positions a rectangle of the given size so that the edges NOT dragged by <paramref name="direction"/>
    /// stay where they were in <paramref name="initialBounds"/> - e.g. a Left/TopLeft/BottomLeft drag keeps
    /// the right edge anchored, a Top/TopLeft/TopRight drag keeps the bottom edge anchored.
    /// </summary>
    public static Rectangle AnchorOppositeEdges(Rectangle initialBounds, WindowResizeDirection direction, Size size)
    {
        var movesLeftEdge = direction is WindowResizeDirection.Left or WindowResizeDirection.TopLeft or WindowResizeDirection.BottomLeft;
        var movesTopEdge = direction is WindowResizeDirection.Top or WindowResizeDirection.TopLeft or WindowResizeDirection.TopRight;
        var x = movesLeftEdge ? initialBounds.Right - size.Width : initialBounds.X;
        var y = movesTopEdge ? initialBounds.Bottom - size.Height : initialBounds.Y;
        return new Rectangle(x, y, size.Width, size.Height);
    }
}
