using System;

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
}