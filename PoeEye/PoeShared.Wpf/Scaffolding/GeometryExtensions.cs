using System;
using System.Drawing;
using System.Windows.Media;
using PoeShared.Native;

namespace PoeShared.Scaffolding;

/// <summary>
///     Common geometry extension methods.
/// </summary>
public static class GeometryExtensions
{
    /// <summary>
    /// Moves a point by the given X and Y values. Useful for shifting objects around.
    /// </summary>
    public static PointF? Offset(this PointF? point, float x, float y)
    {
        if (point == null)
        {
            return null;
        }

        return new PointF(point.Value.X + x, point.Value.Y + y);
    }
    
    /// <summary>
    /// Shrinks a point’s position by dividing its X and Y coordinates. Useful for scaling down.
    /// </summary>
    public static PointF? Divide(this PointF? point, float dividerX, float dividerY)
    {
        if (point == null || dividerX == 0 || dividerY == 0)
        {
            return null; 
        }

        return new PointF(point.Value.X / dividerX, point.Value.Y / dividerY);
    }
    
    /// <summary>
    /// Fixes a rectangle so that its width and height are always positive.
    /// Useful when ensuring a rectangle is drawn properly.
    /// </summary>
    public static WpfRect Normalize(this WpfRect rect)
    {
        var x = rect.Width >= 0
            ? rect.X
            : rect.X + rect.Width;
        var y = rect.Height >= 0
            ? rect.Y
            : rect.Y + rect.Height;
        return new WpfRect
        {
            Width = Math.Abs(rect.Width),
            Height = Math.Abs(rect.Height),
            X = x,
            Y = y
        };
    }
    
    /// <summary>
    /// Ensures the RectangleF has positive width and height by adjusting its position if needed.
    /// This is useful for correcting inverted or negative-sized rectangles.
    /// </summary>
    /// <param name="rect">The RectangleF to normalize.</param>
    /// <returns>A RectangleF with positive width and height.</returns>
    public static RectangleF Normalize(this RectangleF rect)
    {
        var x = rect.Width >= 0 ? rect.X : rect.X + rect.Width;
        var y = rect.Height >= 0 ? rect.Y : rect.Y + rect.Height;

        return new RectangleF(x, y, Math.Abs(rect.Width), Math.Abs(rect.Height));
    }
    
    /// <summary>
    /// Centers a rectangle inside another rectangle.
    /// Useful when positioning objects neatly.
    /// </summary>
    public static WinRect CenterInsideBounds(this WinSize childBounds, WinRect parentBounds)
    {
        // Calculate the center of the parent rectangle
        var parentCenterX = parentBounds.X + parentBounds.Width / 2;
        var parentCenterY = parentBounds.Y + parentBounds.Height / 2;

        // Calculate the top left coordinates of the child rectangle
        var childTopLeftX = parentCenterX - childBounds.Width / 2;
        var childTopLeftY = parentCenterY - childBounds.Height / 2;

        // Create a new rectangle for the child, centered within the parent
        return new WinRect(childTopLeftX, childTopLeftY, childBounds.Width, childBounds.Height);
    }

    /// <summary>
    /// Ensures a rectangle stays within minimum and maximum size limits.
    /// Useful for controlling layout constraints.
    /// </summary>
    public static WinRect EnsureInBounds(this WinRect value, WinSize min, WinSize max)
    {
        return new WinRect(value.Location, value.Size.EnsureInBounds(min, max));
    }
    
    /// <summary>
    /// Ensures a WinSize object stays within specified minimum and maximum size limits.
    /// Useful for scaling constraints in visual layouts.
    /// </summary>
    public static WinSize EnsureInBounds(this WinSize value, WinSize min, WinSize max)
    {
        return new WinSize
        {
            Width = value.Width.EnsureInRange(min.Width, max.Width),
            Height = value.Height.EnsureInRange(min.Height, max.Height)
        };
    }

    /// <summary>
    /// Moves a WPF point inside a given rectangle's boundaries.
    /// If the point is outside the rectangle, it's adjusted to stay within it.
    /// </summary>
    public static WpfPoint EnsureInBounds(this WpfPoint point, WpfRect rect)
    {
        return new WpfPoint()
        {
            X = point.X.EnsureInRange(rect.X, rect.X+rect.Width),
            Y = point.Y.EnsureInRange(rect.Y, rect.Y+rect.Height),
        };
    }

    /// <summary>
    /// Finds the center point of a WPF rectangle.
    /// Useful for positioning objects in the middle of a rectangle.
    /// </summary>
    public static WpfPoint Center(this WpfRect rect)
    {
        return new(rect.Left + (float)rect.Width / 2, rect.Top + (float)rect.Height / 2);
    }
        
    /// <summary>
    /// Finds the center point of a WinRect.
    /// Useful for calculating middle coordinates for layouts or alignment.
    /// </summary>
    public static WinPoint Center(this WinRect point)
    {
        return new((int)Math.Round(point.Left + (float)point.Width / 2), (int)Math.Round(point.Top + (float)point.Height / 2));
    }
    
    /// <summary>
    /// Flips the coordinates of a WinPoint to their opposite values.
    /// Useful for mirroring or reversing positions.
    /// </summary>
    public static WinPoint Negate(this WinPoint point)
    {
        return new WinPoint(-point.X, -point.Y);
    }

    /// <summary>
    /// Returns the overlapping area between two rectangles.
    /// Useful for detecting collisions or calculating shared space.
    /// </summary>
    public static WinRect IntersectWith(this WinRect rect, WinRect otherRectangle)
    {
        var result = rect;
        result.Intersect(otherRectangle);
        return result;
    }

    /// <summary>
    /// Moves a WinPoint by adding another WinPoint's values.
    /// Useful for chaining movement adjustments.
    /// </summary>
    public static WinPoint OffsetBy(this WinPoint point, WinPoint offset)
    {
        var result = point;
        result.Offset(offset);
        return result;
    }

    /// <summary>
    /// Moves a WinPoint by adding custom X and Y values.
    /// Useful for precise positioning adjustments.
    /// </summary>
    public static WinPoint OffsetBy(this WinPoint point, int deltaX, int deltaY)
    {
        return new WinPoint(point.X + deltaX, point.Y + deltaY);
    }

    /// <summary>
    /// Moves a Rectangle by adding another Rectangle’s coordinates and size.
    /// Useful for combining layout changes or calculating offsets.
    /// </summary>
    public static WinRect OffsetBy(this WinRect rect, WinRect offset)
    {
        return new WinRect(rect.X + offset.X, rect.Y + offset.Y, rect.Width + offset.Width, rect.Height + offset.Height);
    }
    
    /// <summary>
    /// Moves a Rectangle by adding a WinPoint's values.
    /// Useful for small position shifts in layouts.
    /// </summary>
    public static WinRect OffsetBy(this WinRect rect, WinPoint offset)
    {
        var result = rect;
        result.Offset(offset);
        return result;
    }

    /// <summary>
    /// Moves a nullable WinRect by adding a nullable WinPoint's values.
    /// Returns null if either input is null.
    /// Useful for conditional positioning logic.
    /// </summary>
    public static WinRect? OffsetBy(this WinRect? rect, WinPoint? offset)
    {
        if (rect == null || offset == null)
        {
            return default;
        }
        return rect.Value with
        {
            X = rect.Value.X + offset.Value.X,
            Y = rect.Value.Y + offset.Value.Y
        };
    }
    
    /// <summary>
    /// Moves a nullable RectangleF by adding a nullable PointF's values.
    /// Returns null if either input is null.
    /// Useful for conditional layout logic.
    /// </summary>
    public static RectangleF? OffsetBy(this RectangleF? rect, PointF? offset)
    {
        if (rect == null || offset == null)
        {
            return default;
        }
        return rect.Value with
        {
            X = rect.Value.X + offset.Value.X,
            Y = rect.Value.Y + offset.Value.Y
        };
    }

    /// <summary>
    /// Fixes a Rectangle by ensuring its width and height are positive.
    /// Useful for correcting invalid sizes that may result from reversed dimensions.
    /// </summary>
    public static WinRect Normalize(this WinRect rect)
    {
        var x = rect.Width >= 0
            ? rect.X
            : rect.X + rect.Width;
        var y = rect.Height >= 0
            ? rect.Y
            : rect.Y + rect.Height;
        return new WinRect
        {
            Width = Math.Abs(rect.Width),
            Height = Math.Abs(rect.Height),
            X = x,
            Y = y
        };
    }
        
    /// <summary>
    /// Computes the effective region representing the bounds inside a source thumbnail of a certain size.
    /// Useful for resizing content while ensuring it fits within defined limits.
    /// </summary>
    public static WinSize FitToSize(this WinRect desiredBounds, WinSize sourceSize)
    {
        try
        {
            var result = desiredBounds;
            var sourceBounds = new WinRect(result.X, result.Y, sourceSize.Width, sourceSize.Height);
            result.Intersect(sourceBounds);
            return result.Size;
        }
        catch (Exception e)
        {
            throw new ApplicationException($"Failed to compute Region size, sourceSize: {sourceSize}, current state: {new { desiredBounds, sourceSize }}", e);
        }
    }
        
    /// <summary>
    /// Confirms that a WinSize object has non-zero width and height.
    /// Useful for verifying dimensions before processing.
    /// </summary>
    public static bool IsNotEmptyArea(this WinSize size)
    {
        return !size.IsEmptyArea();
    }
     
    /// <summary>
    /// Checks if a WinSize object has zero or negative width and height.
    /// Useful for ensuring objects are skipped if they have no meaningful area.
    /// </summary>
    public static bool IsEmptyArea(this WinSize size)
    {
        return size.Width <= 0 || size.Height <= 0;
    }

    /// <summary>
    /// Confirms that a WpfSize object has non-zero width and height.
    /// Useful for verifying dimensions before layout calculations.
    /// </summary>
    public static bool IsNotEmptyArea(this WpfSize size)
    {
        return !size.IsEmptyArea();
    }

    /// <summary>
    /// Checks if a WpfSize object has zero or negative width and height.
    /// Useful for ensuring objects are skipped if they have no meaningful area.
    /// </summary>
    public static bool IsEmptyArea(this WpfSize size)
    {
        return size.Width <= 0 || size.Height <= 0;
    }

    /// <summary>
    /// Checks if a Rect has zero or negative dimensions, meaning no visible area.
    /// Useful for validating layout regions before rendering.
    /// </summary>
    public static bool IsEmptyArea(this WpfRect rect)
    {
        return !IsNotEmptyArea(rect);
    }

    /// <summary>
    /// Checks if a Rectangle has zero or negative dimensions.
    /// Useful when verifying objects before rendering or calculations.
    /// </summary>
    public static bool IsEmptyArea(this WinRect rect)
    {
        return !IsNotEmptyArea(rect);
    }

    /// <summary>
    /// Confirms that a Rectangle has positive width and height values.
    /// Useful for ensuring a rectangle has meaningful dimensions.
    /// </summary>
    public static bool IsNotEmptyArea(this WinRect rect)
    {
        return
            rect.Width > 0 &&
            rect.Height > 0;
    }

    /// <summary>
    /// Checks if a Rectangle is completely empty (default values for all properties).
    /// Useful for quickly detecting uninitialized or empty rectangles.
    /// </summary>
    public static bool IsDefault(this WinRect rect)
    {
        return rect.Width == default && rect.Height == default && rect.X == default && rect.Y == default;
    }

    /// <summary>
    /// Confirms that a Rect has positive width and height values.
    /// Useful for ensuring that a rectangle has meaningful dimensions.
    /// </summary>
    public static bool IsNotEmptyArea(this WpfRect rect)
    {
        return
            rect.Width > 0 &&
            rect.Height > 0;
    }

    /// <summary>
    /// Checks if a Rect has valid positive dimensions and proper coordinates.
    /// Useful for verifying visual layout boundaries.
    /// </summary>
    public static bool IsNotEmpty(this WpfRect rect)
    {
        return
            rect.Width > 0 &&
            rect.Height > 0 &&
            IsFinite(rect.X) &&
            IsFinite(rect.Y) &&
            IsFinite(rect.Width) &&
            IsFinite(rect.Height);
    }
        
    /// <summary>
    /// Checks if a WpfPoint is exactly at coordinate (0, 0).
    /// Useful for identifying points with no meaningful offset.
    /// </summary>
    public static bool IsEmpty(this WpfPoint point)
    {
        return point.X == 0 && point.Y == 0;
    }
        
    /// <summary>
    /// Calculates the total area (width × height) of a WinSize object.
    /// Useful for determining the visual size of content or controls.
    /// </summary>
    public static double Area(this WinSize sourceSize)
    {
        return sourceSize.Height * sourceSize.Width;
    }
    
    /// <summary>
    /// Calculates the total area (width × height) of a SizeF object.
    /// Useful for measuring floating-point sized objects in layouts.
    /// </summary>
    public static double Area(this SizeF sourceSize)
    {
        return sourceSize.Height * sourceSize.Width;
    }
    
    /// <summary>
    /// Calculates the total area (width × height) of a RectangleF object.
    /// Useful for floating-point precision in UI layout calculations.
    /// </summary>
    public static float Area(this RectangleF source)
    {
        return source.Width * source.Height;
    }
    
    /// <summary>
    /// Scales a RectangleF by a given scaling factor for both width and height.
    /// Useful for resizing UI elements or graphical objects.
    /// </summary>
    public static RectangleF Scale(this RectangleF source, SizeF scaleFactor)
    {
        return new RectangleF(source.X * scaleFactor.Width, source.Y * scaleFactor.Height, source.Width * scaleFactor.Width, source.Height * scaleFactor.Height);
    }
        
    /// <summary>
    /// Checks if a System.Drawing.Size has positive width and height.
    /// Useful for ensuring sizes are meaningful before processing.
    /// </summary>
    public static bool IsNotEmpty(this WinSize size)
    {
        return size.Width > 0 &&
               size.Height > 0;
    }

    /// <summary>
    /// Checks if a WpfSize has positive width and height with valid numeric values.
    /// Useful for ensuring layout sizes are appropriate.
    /// </summary>
    public static bool IsNotEmpty(this WpfSize size)
    {
        return size.Width > 0 &&
               size.Height > 0 &&
               IsFinite(size.Width) &&
               IsFinite(size.Height);
    }

    /// <summary>
    /// Converts a System.Drawing.Size into a WpfSize for compatibility with WPF layouts.
    /// Useful when migrating sizes between WinForms and WPF environments.
    /// </summary>
    public static WpfSize ToWpfSize(this WinSize sourceSize)
    {
        return new WpfSize(sourceSize.Width, sourceSize.Height);
    }

    /// <summary>
    /// Converts a WpfSize into a WinSize, rounding down to integer values.
    /// Useful for transitioning sizes into integer-based layout logic.
    /// </summary>
    public static WinSize ToWinSize(this WpfSize sourceSize)
    {
        return new WinSize((int) sourceSize.Width, (int) sourceSize.Height);
    }
        
    /// <summary>
    /// Transforms a WpfPoint into a screen-relative WinPoint.
    /// Useful when calculating screen coordinates for UI placement.
    /// </summary>
    public static WinPoint ToScreen(this WpfPoint source, Visual owner)
    {
        return owner.PointToScreen(source).ToWinPoint();
    }

    /// <summary>
    /// Transforms a Rect into a screen-relative Rectangle.
    /// Useful for ensuring UI elements align correctly in multi-screen setups.
    /// </summary>
    public static WinRect ToScreen(this WpfRect sourceSize, Visual owner)
    {
        var ownerTopLeft = owner.PointToScreen(new WpfPoint(0, 0));
        var topLeft = owner.PointToScreen(sourceSize.TopLeft);
        topLeft.Offset(-ownerTopLeft.X, -ownerTopLeft.Y);
        var bottomRight = owner.PointToScreen(sourceSize.BottomRight);
        bottomRight.Offset(-ownerTopLeft.X, -ownerTopLeft.Y);
        var relative = new WpfRect(topLeft, bottomRight);
        return relative.ToWinRectangle();
    }
        
    /// <summary>
    /// Transforms a WpfSize into a screen-relative WinSize.
    /// Useful for calculating scaled layout sizes on different screens.
    /// </summary>
    public static WinSize ToScreen(this WpfSize sourceSize, Visual owner)
    {
        var ownerTopLeft = owner.PointToScreen(new WpfPoint(0, 0));
        var bottomRight = owner.PointToScreen(new WpfPoint(sourceSize.Width, sourceSize.Height));
        var relative = new WpfSize(bottomRight.X - ownerTopLeft.X, bottomRight.Y - ownerTopLeft.Y);
        return relative.ToWinSize();
    }

    /// <summary>
    /// Scales a WpfPoint to screen coordinates using the desktop DPI for scaling.
    /// Useful for adjusting point positions when scaling UI elements.
    /// </summary>
    public static WinPoint ScaleToScreen(this WpfPoint sourceSize)
    {
        var dpi = UnsafeNative.GetDesktopDpi();
        return ScaleToScreen(sourceSize, dpi);
    }
        
    /// <summary>
    /// Scales a WpfPoint to screen coordinates using a specified DPI.
    /// Useful for calculating precise point positions based on DPI scaling.
    /// </summary>
    public static WinPoint ScaleToScreen(this WpfPoint sourceSize, PointF dpi)
    {
        var result = new WinPoint((int)(sourceSize.X * dpi.X), (int)(sourceSize.Y * dpi.Y));
        return result;
    }

    /// <summary>
    /// Scales a Rect to screen coordinates using the default desktop DPI.
    /// Useful for scaling layout regions to fit high-DPI displays.
    /// </summary>
    public static WinRect ScaleToScreen(this WpfRect sourceSize)
    {
        return ScaleToScreen(sourceSize, UnsafeNative.GetDesktopWindow());
    }
        
    /// <summary>
    /// Scales a Rect to screen coordinates using DPI from a specific desktop handle.
    /// Useful for handling multiple displays with different DPI settings.
    /// </summary>
    public static WinRect ScaleToScreen(this WpfRect sourceSize, IntPtr hDesktop)
    {
        if (sourceSize.IsEmpty)
        {
            return WinRect.Empty;
        }
        var dpi = UnsafeNative.GetDesktopDpi(hDesktop);
        return ScaleToScreen(sourceSize, dpi);
    }
        
    /// <summary>
    /// Scales a Rect to screen coordinates using a specified DPI.
    /// Useful for calculating precise layout regions for different DPI settings.
    /// </summary>
    public static WinRect ScaleToScreen(this WpfRect sourceSize, PointF dpi)
    {
        return new WinRect((int)(sourceSize.X * dpi.X), (int)(sourceSize.Y * dpi.Y), (int)(sourceSize.Width * dpi.X), (int)(sourceSize.Height * dpi.Y));
    }

    /// <summary>
    /// Scales a WpfSize to screen coordinates using the desktop DPI for scaling.
    /// Useful for converting layout sizes to fit high-DPI displays.
    /// </summary>
    public static WinSize ScaleToScreen(this WpfSize sourceSize)
    {
        var dpi = UnsafeNative.GetDesktopDpi();
        return ScaleToScreen(sourceSize, dpi);
    }
        
    /// <summary>
    /// Scales a WpfSize to screen coordinates using a specified DPI.
    /// Useful for adjusting layout sizes for precise DPI scaling.
    /// </summary>
    public static WinSize ScaleToScreen(this WpfSize sourceSize, PointF dpi)
    {
        return new WinSize((int)(sourceSize.Width * dpi.X), (int)(sourceSize.Height * dpi.Y));
    }
        
    /// <summary>
    /// Scales a WinSize by given width and height multipliers.
    /// Useful for proportionally resizing UI elements.
    /// </summary>
    public static WinSize Scale(this WinSize sourceSize, float factorX, float factorY)
    {
        return new WinSize((int)(sourceSize.Width * factorX), (int)(sourceSize.Height * factorY));
    }

    /// <summary>
    /// Scales a WinRect to WPF coordinates using a specified device pixel ratio.
    /// Useful for converting screen coordinates to WPF layouts.
    /// </summary>
    public static WpfRect ScaleToWpf(this WinRect sourceSize, float devicePixelRatio)
    {
        return ScaleToWpf(sourceSize, new PointF(devicePixelRatio, devicePixelRatio));
    }
    
    /// <summary>
    /// Scales a WinRect to WPF coordinates using a specified DPI value.
    /// Useful for accurate scaling when switching between display types.
    /// </summary>
    public static WpfRect ScaleToWpf(this WinRect sourceSize, PointF dpi)
    {
        var result = sourceSize.ToWpfRectangle();
        result.Scale(1 / dpi.X, 1 / dpi.Y);
        return result;
    }
        
    /// <summary>
    /// Scales a WinRect to WPF coordinates using the desktop DPI for scaling.
    /// Useful for automatic DPI-based scaling adjustments.
    /// </summary>
    public static WpfRect ScaleToWpf(this WinRect sourceSize)
    {
        var dpi = UnsafeNative.GetDesktopDpi();
        return ScaleToWpf(sourceSize, dpi);
    }
        
    /// <summary>
    /// Scales a WinPoint to WPF coordinates using the desktop DPI for scaling.
    /// Useful for converting point positions to fit WPF layouts.
    /// </summary>
    public static WpfPoint ScaleToWpf(this WinPoint source)
    {
        var dpi = UnsafeNative.GetDesktopDpi();
        return ScaleToWpf(source, dpi);
    }
    
    /// <summary>
    /// Scales a WinPoint to WPF coordinates using a specified device pixel ratio.
    /// Useful for adjusting point coordinates based on scaling factors.
    /// </summary>
    public static WpfPoint ScaleToWpf(this WinPoint sourceSize, float devicePixelRatio)
    {
        return ScaleToWpf(sourceSize, new PointF(devicePixelRatio, devicePixelRatio));
    }
    
    /// <summary>
    /// Scales a WinPoint to WPF coordinates using a specified DPI value.
    /// Useful for achieving precise point placement in high-DPI environments.
    /// </summary>
    public static WpfPoint ScaleToWpf(this WinPoint source, PointF dpi)
    {
        return new WpfPoint(source.X / dpi.X, source.Y / dpi.Y);
    }
        
    /// <summary>
    /// Scales a WinRect by dividing its position and size by a specified DPI value.
    /// Useful for resizing and positioning elements in DPI-aware applications.
    /// </summary>
    public static WinRect Scale(this WinRect sourceSize, double dpi)
    {
        return new WinRect((int)(sourceSize.X / dpi), (int)(sourceSize.Y / dpi), (int)(sourceSize.Width / dpi), (int)(sourceSize.Height / dpi));
    }
        
    /// <summary>
    /// Expands a WinRect by a given width and height multiplier.
    /// Useful for enlarging rectangles while maintaining proportional scaling.
    /// </summary>
    public static WinRect InflateScale(this WinRect sourceSize, float widthMultiplier, float heightMultiplier)
    {
        var result = sourceSize;

        var deltaWidth = (int)((result.Width * widthMultiplier - sourceSize.Width) / 2);
        var deltaHeight = (int)((result.Height * heightMultiplier - sourceSize.Height) / 2);

        result.Inflate(deltaWidth, deltaHeight);
        return result;
    }
        
    /// <summary>
    /// Expands a WinRect by adding specific width and height values.
    /// Useful for adding padding or increasing the size of a rectangle.
    /// </summary>
    public static WinRect InflateSize(this WinRect sourceSize, int width, int height)
    {
        var result = sourceSize;
        result.Inflate(width, height);
        return result;
    }

    /// <summary>
    /// Converts a WPF Rect into a System.Drawing Rectangle.
    /// Useful for bridging WPF and System.Drawing APIs.
    /// </summary>
    public static WinRect ToWinRectangle(this WpfRect sourceSize)
    {
        return new WinRect
        {
            X = (int) sourceSize.X,
            Y = (int) sourceSize.Y,
            Width = (int) sourceSize.Width,
            Height = (int) sourceSize.Height
        };
    }
    
    /// <summary>
    /// Converts a RectangleF into a System.Drawing Rectangle.
    /// Useful for rounding float-based dimensions into integer-based Rectangle values.
    /// </summary>
    public static WinRect ToWinRectangle(this RectangleF sourceSize)
    {
        return new WinRect
        {
            X = (int) sourceSize.X,
            Y = (int) sourceSize.Y,
            Width = (int) sourceSize.Width,
            Height = (int) sourceSize.Height
        };
    }
        
    /// <summary>
    /// Converts a System.Drawing Point into a WPF Point.
    /// Useful for adapting point coordinates between UI frameworks.
    /// </summary>
    public static WpfPoint ToWpfPoint(this WinPoint source)
    {
        return new WpfPoint
        {
            X = source.X,
            Y = source.Y
        };
    }

    /// <summary>
    /// Converts a WPF Point into a System.Drawing Point.
    /// Useful for adapting point coordinates between UI frameworks.
    /// </summary>
    public static WinPoint ToWinPoint(this WpfPoint source)
    {
        return new WinPoint
        {
            X = (int) source.X,
            Y = (int) source.Y
        };
    }
    
    /// <summary>
    /// Converts a PointF into a WPF Point.
    /// Useful for converting floating-point coordinates into WPF coordinate space.
    /// </summary>
    public static WpfPoint ToWpfPoint(this PointF point)
    {
        return new WpfPoint(point.X, point.Y);
    }
    
    /// <summary>
    /// Converts a WPF Point into a PointF.
    /// Useful for converting WPF coordinates to floating-point values for precision.
    /// </summary>
    public static PointF ToPointF(this WpfPoint point)
    {
        return new PointF((float)point.X, (float)point.Y);
    }

    /// <summary>
    /// Converts a System.Drawing Rectangle into a WPF Rect.
    /// Useful for bridging WPF and System.Drawing APIs.
    /// </summary>
    public static WpfRect ToWpfRectangle(this WinRect sourceSize)
    {
        return new WpfRect
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