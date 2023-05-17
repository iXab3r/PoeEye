using System;
using System.Drawing;
using System.Windows;
using System.Windows.Media;
using PoeShared.Native;

namespace PoeShared.Scaffolding;

/// <summary>
///     Common geometry extension methods.
/// </summary>
public static class GeometryExtensions
{
    public static Rect Normalize(this Rect rect)
    {
        var x = rect.Width >= 0
            ? rect.X
            : rect.X + rect.Width;
        var y = rect.Height >= 0
            ? rect.Y
            : rect.Y + rect.Height;
        return new Rect
        {
            Width = Math.Abs(rect.Width),
            Height = Math.Abs(rect.Height),
            X = x,
            Y = y
        };
    }
    
    public static WinRect CenterInsideBounds(this WinRect childBounds, WinRect parentBounds)
    {
        // Calculate the center of the parent rectangle
        var parentCenterX = parentBounds.X + parentBounds.Width / 2;
        var parentCenterY = parentBounds.Y + parentBounds.Height / 2;

        // Calculate the top left coordinates of the child rectangle
        var childTopLeftX = parentCenterX - childBounds.Width / 2;
        var childTopLeftY = parentCenterY - childBounds.Height / 2;

        // Create a new rectangle for the child, centered within the parent
        var centeredChildBounds = childBounds with {X = childTopLeftX, Y = childTopLeftY};
        return centeredChildBounds;
    }

    public static WinRect EnsureInBounds(this WinRect value, WinSize min, WinSize max)
    {
        return new WinRect(value.Location, value.Size.EnsureInBounds(min, max));
    }
    
    public static WinSize EnsureInBounds(this WinSize value, WinSize min, WinSize max)
    {
        return new WinSize
        {
            Width = value.Width.EnsureInRange(min.Width, max.Width),
            Height = value.Height.EnsureInRange(min.Height, max.Height)
        };
    }

    public static WpfPoint Center(this Rect rect)
    {
        return new(rect.Left + (float)rect.Width / 2, rect.Top + (float)rect.Height / 2);
    }
        
    public static WinPoint Center(this WinRect point)
    {
        return new((int)Math.Round(point.Left + (float)point.Width / 2), (int)Math.Round(point.Top + (float)point.Height / 2));
    }
    
    public static WinPoint Negate(this WinPoint point)
    {
        return new WinPoint(-point.X, -point.Y);
    }
    
    public static WinPoint OffsetBy(this WinPoint point, WinPoint offset)
    {
        var result = point;
        result.Offset(offset);
        return result;
    }

    public static Rectangle IntersectWith(this Rectangle rect, Rectangle otherRectangle)
    {
        var result = rect;
        result.Intersect(otherRectangle);
        return result;
    }

    public static Rectangle OffsetBy(this Rectangle rect, WinPoint offset)
    {
        var result = rect;
        result.Offset(offset);
        return result;
    }

    public static Rectangle Normalize(this Rectangle rect)
    {
        var x = rect.Width >= 0
            ? rect.X
            : rect.X + rect.Width;
        var y = rect.Height >= 0
            ? rect.Y
            : rect.Y + rect.Height;
        return new Rectangle
        {
            Width = Math.Abs(rect.Width),
            Height = Math.Abs(rect.Height),
            X = x,
            Y = y
        };
    }
        
    /// <summary>
    ///     Computes the effective region representing the bounds inside a source thumbnail of a certain size.
    /// </summary>
    public static WinSize FitToSize(this WinRect desiredBounds, WinSize sourceSize)
    {
        try
        {
            var result = desiredBounds;
            var sourceBounds = new Rectangle(result.X, result.Y, sourceSize.Width, sourceSize.Height);
            result.Intersect(sourceBounds);
            return result.Size;
        }
        catch (Exception e)
        {
            throw new ApplicationException($"Failed to compute Region size, sourceSize: {sourceSize}, current state: {new { desiredBounds, sourceSize }}", e);
        }
    }
        
    public static bool IsNotEmptyArea(this WinSize size)
    {
        return !size.IsEmptyArea();
    }
        
    public static bool IsEmptyArea(this WinSize size)
    {
        return size.Width <= 0 || size.Height <= 0;
    }

    public static bool IsNotEmptyArea(this WpfSize size)
    {
        return !size.IsEmptyArea();
    }
        
    public static bool IsEmptyArea(this WpfSize size)
    {
        return size.Width <= 0 || size.Height <= 0;
    }
        
    public static bool IsDefault(this Rectangle rect)
    {
        return rect.Width == default && rect.Height == default && rect.X == default && rect.Y == default;
    }
        
    public static bool IsEmptyArea(this Rect rect)
    {
        return !IsNotEmptyArea(rect);
    }
        
    public static bool IsNotEmptyArea(this Rect rect)
    {
        return
            rect.Width > 0 &&
            rect.Height > 0;
    }

    public static bool IsEmptyArea(this Rectangle rect)
    {
        return !IsNotEmptyArea(rect);
    }
        
    public static bool IsNotEmptyArea(this Rectangle rect)
    {
        return
            rect.Width > 0 &&
            rect.Height > 0;
    }
        
    public static bool IsNotEmpty(this Rect rect)
    {
        return
            rect.Width > 0 &&
            rect.Height > 0 &&
            IsFinite(rect.X) &&
            IsFinite(rect.Y) &&
            IsFinite(rect.Width) &&
            IsFinite(rect.Height);
    }
        
    public static bool IsEmpty(this WpfPoint point)
    {
        return point.X == 0 && point.Y == 0;
    }
        
    public static double Area(this WinSize sourceSize)
    {
        return sourceSize.Height * sourceSize.Width;
    }
        
    public static bool IsNotEmpty(this System.Drawing.Size size)
    {
        return size.Width > 0 &&
               size.Height > 0;
    }

    public static bool IsNotEmpty(this WpfSize size)
    {
        return size.Width > 0 &&
               size.Height > 0 &&
               IsFinite(size.Width) &&
               IsFinite(size.Height);
    }

    public static WinSize ToWpfSize(this System.Drawing.Size sourceSize)
    {
        return new WinSize(sourceSize.Width, sourceSize.Height);
    }

    public static WinSize ToWinSize(this WpfSize sourceSize)
    {
        return new WinSize((int) sourceSize.Width, (int) sourceSize.Height);
    }
        
    public static WinPoint ToScreen(this WpfPoint source, Visual owner)
    {
        return owner.PointToScreen(source).ToWinPoint();
    }

    public static Rectangle ToScreen(this Rect sourceSize, Visual owner)
    {
        var ownerTopLeft = owner.PointToScreen(new WpfPoint(0, 0));
        var topLeft = owner.PointToScreen(sourceSize.TopLeft);
        topLeft.Offset(-ownerTopLeft.X, -ownerTopLeft.Y);
        var bottomRight = owner.PointToScreen(sourceSize.BottomRight);
        bottomRight.Offset(-ownerTopLeft.X, -ownerTopLeft.Y);
        var relative = new Rect(topLeft, bottomRight);
        return relative.ToWinRectangle();
    }
        
    public static WinSize ToScreen(this WpfSize sourceSize, Visual owner)
    {
        var ownerTopLeft = owner.PointToScreen(new WpfPoint(0, 0));
        var bottomRight = owner.PointToScreen(new WpfPoint(sourceSize.Width, sourceSize.Height));
        var relative = new WpfSize(bottomRight.X - ownerTopLeft.X, bottomRight.Y - ownerTopLeft.Y);
        return relative.ToWinSize();
    }

    public static WinPoint ScaleToScreen(this WpfPoint sourceSize)
    {
        var dpi = UnsafeNative.GetDesktopDpi();
        return ScaleToScreen(sourceSize, dpi);
    }
        
    public static WinPoint ScaleToScreen(this WpfPoint sourceSize, PointF dpi)
    {
        var result = new WinPoint((int)(sourceSize.X * dpi.X), (int)(sourceSize.Y * dpi.Y));
        return result;
    }

    public static WinRect ScaleToScreen(this Rect sourceSize)
    {
        return ScaleToScreen(sourceSize, UnsafeNative.GetDesktopWindow());
    }
        
    public static WinRect ScaleToScreen(this Rect sourceSize, IntPtr hDesktop)
    {
        if (sourceSize.IsEmpty)
        {
            return WinRect.Empty;
        }
        var dpi = UnsafeNative.GetDesktopDpi(hDesktop);
        return ScaleToScreen(sourceSize, dpi);
    }
        
    public static WinRect ScaleToScreen(this Rect sourceSize, PointF dpi)
    {
        return new WinRect((int)(sourceSize.X * dpi.X), (int)(sourceSize.Y * dpi.Y), (int)(sourceSize.Width * dpi.X), (int)(sourceSize.Height * dpi.Y));
    }

    public static WinSize ScaleToScreen(this WpfSize sourceSize)
    {
        var dpi = UnsafeNative.GetDesktopDpi();
        return ScaleToScreen(sourceSize, dpi);
    }
        
    public static WinSize ScaleToScreen(this WpfSize sourceSize, PointF dpi)
    {
        return new WinSize((int)(sourceSize.Width * dpi.X), (int)(sourceSize.Height * dpi.Y));
    }
        
    public static WinSize Scale(this WinSize sourceSize, float factorX, float factorY)
    {
        return new WinSize((int)(sourceSize.Width * factorX), (int)(sourceSize.Height * factorY));
    }

    public static Rect ScaleToWpf(this WinRect sourceSize, PointF dpi)
    {
        var result = sourceSize.ToWpfRectangle();
        result.Scale(1 / dpi.X, 1 / dpi.Y);
        return result;
    }
        
    public static Rect ScaleToWpf(this WinRect sourceSize)
    {
        var dpi = UnsafeNative.GetDesktopDpi();
        return ScaleToWpf(sourceSize, dpi);
    }
        
    public static WpfPoint ScaleToWpf(this WinPoint source)
    {
        var dpi = UnsafeNative.GetDesktopDpi();
        return new WpfPoint(source.X / dpi.X, source.Y / dpi.Y);
    }
        
    public static WinRect Scale(this WinRect sourceSize, double dpi)
    {
        return new WinRect((int)(sourceSize.X / dpi), (int)(sourceSize.Y / dpi), (int)(sourceSize.Width / dpi), (int)(sourceSize.Height / dpi));
    }
        
    public static WinRect InflateScale(this WinRect sourceSize, float widthMultiplier, float heightMultiplier)
    {
        var result = sourceSize;
        result.Inflate((int)(result.Width * widthMultiplier), (int)(result.Height * heightMultiplier));
        return result;
    }
        
    public static WinRect InflateSize(this WinRect sourceSize, int width, int height)
    {
        var result = sourceSize;
        result.Inflate(result.Width, result.Height);
        return result;
    }

    public static Rectangle ToWinRectangle(this Rect sourceSize)
    {
        return new Rectangle
        {
            X = (int) sourceSize.X,
            Y = (int) sourceSize.Y,
            Width = (int) sourceSize.Width,
            Height = (int) sourceSize.Height
        };
    }
        
    public static WpfPoint ToWpfPoint(this System.Drawing.Point source)
    {
        return new WpfPoint
        {
            X = source.X,
            Y = source.Y
        };
    }

    public static System.Drawing.Point ToWinPoint(this WpfPoint source)
    {
        return new System.Drawing.Point
        {
            X = (int) source.X,
            Y = (int) source.Y
        };
    }

    public static Rect ToWpfRectangle(this Rectangle sourceSize)
    {
        return new Rect
        {
            X = sourceSize.X,
            Y = sourceSize.Y,
            Width = sourceSize.Width,
            Height = sourceSize.Height
        };
    }

    private static bool IsFinite(double value)
    {
        return !double.IsInfinity(value);
    }
}