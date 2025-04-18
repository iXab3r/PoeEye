using System;
using System.Numerics;

namespace PoeShared.Scaffolding;

public static class GeometryExtensions
{
    /// <summary>
    /// Adjusts the given bounds to fit inside the specified region. If the width or height of the region is non-positive, 
    /// it represents how much of the bounds' width or height to remove, respectively. 
    /// If the region's x or y is set, it represents the offset inside the bounds.
    /// </summary>
    /// <param name="bounds">The original bounds that need to be adjusted.</param>
    /// <param name="region">The region inside which the bounds need to fit.</param>
    /// <returns>The adjusted bounds that fit inside the specified region.</returns>
    public static WinRect PickRegion(this WinRect bounds, WinRect region)
    {
        if (region.IsEmpty)
        {
            return bounds;
        }

        var boundsAfterOffset = bounds;
        boundsAfterOffset.Offset(region.Location);
        boundsAfterOffset.Intersect(bounds);
        
        var adjustedWidth = region.Width <= 0 ? Math.Max(0, region.Width + boundsAfterOffset.Width) : region.Width;
        var adjustedHeight = region.Height <= 0 ? Math.Max(0, region.Height + boundsAfterOffset.Height) : region.Height;
        var adjustedRegion = new WinRect(region.Left, region.Top, adjustedWidth, adjustedHeight);
        adjustedRegion.Intersect(bounds);
        return adjustedRegion;
    }

    public static WinRect Transform(this WinRect bounds, Matrix3x2 transformationMatrix)
    {
        var topLeft = Vector2.Transform(new Vector2(bounds.Left, bounds.Top), transformationMatrix);
        var bottomRight = Vector2.Transform(new Vector2(bounds.Right, bounds.Bottom), transformationMatrix);
        return WinRect.FromLTRB((int)topLeft.X, (int)topLeft.Y, (int)bottomRight.X, (int)bottomRight.Y);
    }
    
    public static WinPoint Transform(this WinPoint point, Matrix3x2 transformationMatrix)
    {
        var vector = Vector2.Transform(new Vector2(point.X, point.Y), transformationMatrix);
        return new WinPoint((int)vector.X, (int)vector.Y);
    }
      
    public static Vector2 Transform(this Vector2 vector, Matrix3x2 transformationMatrix)
    {
        return Vector2.Transform(vector, transformationMatrix);
    }
    
    public static WinRectangleF Transform(this WinRectangleF bounds, Matrix3x2 transformationMatrix)
    {
        var topLeft = Vector2.Transform(new Vector2(bounds.Left, bounds.Top), transformationMatrix);
        var bottomRight = Vector2.Transform(new Vector2(bounds.Right, bounds.Bottom), transformationMatrix);
        return WinRectangleF.FromLTRB((int)topLeft.X, (int)topLeft.Y, (int)bottomRight.X, (int)bottomRight.Y);
    }
    
    /// <summary>
    /// Determine the center of a Rectangle.
    /// </summary>
    /// <param name="value">The rectangle to perform the calculation against.</param>
    /// <returns>The point of the center of the rectangle.</returns>
    public static WinPointF Center(this WinRectangleF value)
    {
        return new(value.X + value.Width / 2.0f, value.Y + value.Height / 2.0f);
    }
}