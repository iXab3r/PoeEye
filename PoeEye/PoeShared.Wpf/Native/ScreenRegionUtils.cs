using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using PoeShared.Scaffolding;

namespace PoeShared.Native;

public static class ScreenRegionUtils
{
    public static WinRect CalculateProjection(
        WpfRect selection,
        WpfSize selectorSize,
        WinRect projectionAreaBounds,
        bool allowProjectionOutsideArea)
    {
        var result = CalculateProjection(selection, selectorSize, projectionAreaBounds.Size, allowProjectionOutsideArea);
        result.Offset(projectionAreaBounds.Location);
        return result;
    }
    
    public static WinRect CalculateProjection(
        WpfRect selection,
        WpfSize selectorSize,
        WinRect projectionAreaBounds)
    {
        return CalculateProjection(selection, selectorSize, projectionAreaBounds, allowProjectionOutsideArea: false);
    }

    public static WinPoint CalculateProjection(
        WinPoint point,
        WinSize selectorSize,
        WinSize projectionAreaSize)
    {
        return CalculateProjection(new WpfRect(new WpfPoint(point.X, point.Y), new WpfSize(1, 1)), selectorSize.ToWpfSize(), projectionAreaSize).Location;
    }

    public static Rectangle CalculateProjection(
        WpfRect selection,
        WpfSize selectorSize,
        WinSize projectionAreaSize)
    {
        return CalculateProjection(selection, selectorSize, projectionAreaSize, allowProjectionOutsideArea: false);
    }

    public static Rectangle CalculateProjection(
        WpfRect selection,
        WpfSize selectorSize,
        WinSize projectionAreaSize,
        bool allowProjectionOutsideArea)
    {
        if (selection.IsEmptyArea() || selectorSize.IsEmptyArea() || projectionAreaSize.IsEmptyArea())
        {
            return Rectangle.Empty;
        }
            
        var selectionPercent = new WpfRect
        {
            X = selection.X / selectorSize.Width,
            Y = selection.Y / selectorSize.Height,
            Height = selection.Height / selectorSize.Height,
            Width = selection.Width / selectorSize.Width
        };
            
        var destinationRegion = new WpfRect
        {
            X = selectionPercent.X * projectionAreaSize.Width,
            Y = selectionPercent.Y * projectionAreaSize.Height,
            Width = selectionPercent.Width * projectionAreaSize.Width,
            Height = selectionPercent.Height * projectionAreaSize.Height
        };
        var result = new WinRect
        {
            X = ((int)Math.Floor(destinationRegion.X)),
            Y = (int)Math.Floor(destinationRegion.Y),
            Width = (int)Math.Ceiling(destinationRegion.Width),
            Height = (int)Math.Ceiling(destinationRegion.Height),
        };

        var projectionBounds = new Rectangle(WinPoint.Empty, projectionAreaSize);
        result.Intersect(projectionBounds);
        if (allowProjectionOutsideArea || result.IntersectsWith(projectionBounds))
        {
            return result;
        }

        return Rectangle.Empty;
    }
    
    public static WpfRect ReverseProjection(
        WinRect projectedSelection,
        WpfSize selectorSize,
        WinRect projectionAreaBounds)
    {
        var offset = new WinPoint(-projectionAreaBounds.Location.X, -projectionAreaBounds.Location.Y);
        var result = ReverseProjection(projectedSelection.OffsetBy(offset), selectorSize, projectionAreaBounds.Size);
        return result;
    }

    public static WpfRect ReverseProjection(
        WinRect projectedSelection,
        WpfSize selectorSize,
        WinSize projectionAreaSize)
    {
        if (projectedSelection.IsEmptyArea() || selectorSize.IsEmptyArea() || projectionAreaSize.IsEmptyArea())
        {
            return new WpfRect();
        }
            
        var selectionPercent = new WpfRect
        {
            X = (float)projectedSelection.X / projectionAreaSize.Width,
            Y = (float)projectedSelection.Y / projectionAreaSize.Height,
            Height = (float)projectedSelection.Height / projectionAreaSize.Height,
            Width = (float)projectedSelection.Width / projectionAreaSize.Width
        };
            
        var destinationRegion = new WpfRect
        {
            X = selectionPercent.X * selectorSize.Width,
            Y = selectionPercent.Y * selectorSize.Height,
            Width = selectionPercent.Width * selectorSize.Width,
            Height = selectionPercent.Height * selectorSize.Height
        };

        var result = new WpfRect(default, selectorSize);
        result.Intersect(destinationRegion);
        return !result.IntersectsWith(destinationRegion) ? new WpfRect() : result;
    }

    public static WinPoint ToScreenCoordinates(double absoluteX, double absoluteY)
    {
        return ToScreenCoordinates(absoluteX, absoluteY, SystemInformation.VirtualScreen);
    }

    public static WinPoint ToScreenCoordinates(double absoluteX, double absoluteY, Rectangle screenBounds)
    {
        return new WinPoint(
            ToScreenCoordinates(absoluteX, screenBounds.X, screenBounds.Width),
            ToScreenCoordinates(absoluteY, screenBounds.Y, screenBounds.Height));
    }

    public static WinPoint ToScreenCoordinates(PointF winInputCoordinates)
    {
        return ToScreenCoordinates(winInputCoordinates.X, winInputCoordinates.Y);
    }

    public static (double X, double Y) ToWinInputCoordinates(WinPoint screenCoordinates, Rectangle screenBounds)
    {
        return (ToWinInputCoordinates(screenCoordinates.X, screenBounds.X, screenBounds.Width), ToWinInputCoordinates(screenCoordinates.Y, screenBounds.Y, screenBounds.Height));
    }

    public static (double X, double Y) ToWinInputCoordinates(WinPoint screenCoordinates)
    {
        return ToWinInputCoordinates(screenCoordinates, SystemInformation.VirtualScreen);
    }

    private static int ToScreenCoordinates(double absolute, int offset, int size)
    {
        return (int)Math.Round(absolute / 65535 * size + offset - (absolute < 0 ? -1 : 1));
    }

    private static double ToWinInputCoordinates(int coord, int offset, int size)
    {
        // from https://github.com/Lexikos/AutoHotkey_L/blob/83323202a7e48350a5e8a514e1cdd2f4de1f5977/source/keyboard_mouse.cpp

        // Convert the specified screen coordinates to mouse event coordinates (MOUSEEVENTF_ABSOLUTE).
        // MSDN: "In a multimonitor system, [MOUSEEVENTF_ABSOLUTE] coordinates map to the primary monitor."
        // The above implies that values greater than 65535 or less than 0 are appropriate, but only on
        // multi-monitor systems.  For simplicity, performance, and backward compatibility, no check for
        // multi-monitor is done here. Instead, the system's default handling for out-of-bounds coordinates
        // is used; for example, mouse_event() stops the cursor at the edge of the screen.
        // UPDATE: In v1.0.43, the following formula was fixed (actually broken, see below) to always yield an
        // in-range value. The previous formula had a +1 at the end:
        // aX|Y = ((65535 * aX|Y) / (screen_width|height - 1)) + 1;
        // The extra +1 would produce 65536 (an out-of-range value for a single-monitor system) if the maximum
        // X or Y coordinate was input (e.g. x-position 1023 on a 1024x768 screen).  Although this correction
        // seems inconsequential on single-monitor systems, it may fix certain misbehaviors that have been reported
        // on multi-monitor systems. Update: according to someone I asked to test it, it didn't fix anything on
        // multimonitor systems, at least those whose monitors vary in size to each other.  In such cases, he said
        // that only SendPlay or DllCall("SetCursorPos") make mouse movement work properly.
        // FIX for v1.0.44: Although there's no explanation yet, the v1.0.43 formula is wrong and the one prior
        // to it was correct; i.e. unless +1 is present below, a mouse movement to coords near the upper-left corner of
        // the screen is typically off by one pixel (only the y-coordinate is affected in 1024x768 resolution, but
        // in other resolutions both might be affected).
        // v1.0.44.07: The following old formula has been replaced:
        // (((65535 * coord) / (width_or_height - 1)) + 1)
        // ... with the new one below.  This is based on numEric's research, which indicates that mouse_event()
        // uses the following inverse formula internally:
        // x_or_y_coord = (x_or_y_abs_coord * screen_width_or_height) / 65536

        return ((double)coord + (coord < 0 ? -1 : 1) - offset) / size * 65535;
    }
}