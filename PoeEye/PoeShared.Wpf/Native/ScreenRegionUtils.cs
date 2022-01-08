using System;
using System.Drawing;
using System.Windows.Forms;
using PoeShared.Scaffolding;
using WinRect = System.Drawing.Rectangle;
using WinSize = System.Drawing.Size;
using WpfSize = System.Windows.Size;
using WpfRect = System.Windows.Rect;

namespace PoeShared.Native
{
    public static class ScreenRegionUtils
    {
        public static Rectangle CalculateBounds(Rectangle selection,
            Size selectorSize,
            Rectangle clientBounds)
        {
            return CalculateBounds(selection, selectorSize, clientBounds, Rectangle.Empty);
        }

        public static Rectangle CalculateProjection(WpfRect selection,
            WpfSize selectorSize,
            WinRect projectionAreaBounds)
        {
            var result = CalculateProjection(selection, selectorSize, projectionAreaBounds.Size);
            result.Offset(projectionAreaBounds.Location);
            return result;
        }

        public static Rectangle CalculateProjection(WpfRect selection,
            WpfSize selectorSize,
            WinSize projectionAreaSize)
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

            var projectionBounds = new Rectangle(Point.Empty, projectionAreaSize);
            result.Intersect(projectionBounds);
            return !result.IntersectsWith(projectionBounds) ? Rectangle.Empty : result;
        }

        [Obsolete("Replaced by CalculateProjection")]
        public static Rectangle CalculateBounds(Rectangle selection,
            Size selectorSize,
            Rectangle clientBounds,
            Rectangle clientRegionBounds)
        {
            if (selection.IsEmpty || selectorSize.IsEmpty || clientBounds.IsEmpty)
            {
                return Rectangle.Empty;
            }

            var currentTargetRegion = clientBounds;
            if (!clientRegionBounds.IsEmpty)
            {
                currentTargetRegion.Intersect(clientRegionBounds);
            }

            var selectionPercent = new WpfRect
            {
                X = (double)selection.X / selectorSize.Width,
                Y = (double)selection.Y / selectorSize.Height,
                Height = (double)selection.Height / selectorSize.Height,
                Width = (double)selection.Width / selectorSize.Width
            };

            WpfRect currentRegionPercent;
            if (currentTargetRegion.IsNotEmptyArea())
            {
                currentRegionPercent = new WpfRect
                {
                    X = (double)currentTargetRegion.X / clientBounds.Width,
                    Y = (double)currentTargetRegion.Y / clientBounds.Height,
                    Height = (double)currentTargetRegion.Height / clientBounds.Height,
                    Width = (double)currentTargetRegion.Width / clientBounds.Width
                };
            }
            else
            {
                currentRegionPercent = new WpfRect
                {
                    Width = 1,
                    Height = 1
                };
            }

            var destinationRegion = new WpfRect
            {
                X = (currentRegionPercent.X + selectionPercent.X * currentRegionPercent.Width) * clientBounds.Width,
                Y = (currentRegionPercent.Y + selectionPercent.Y * currentRegionPercent.Height) * clientBounds.Height,
                Width = Math.Max(1, currentRegionPercent.Width * selectionPercent.Width * clientBounds.Width),
                Height = Math.Max(1, currentRegionPercent.Height * selectionPercent.Height * clientBounds.Height)
            };
            destinationRegion = new WpfRect
            {
                X = Math.Round(destinationRegion.X),
                Y = Math.Round(destinationRegion.Y),
                Width = Math.Round(destinationRegion.Width),
                Height = Math.Round(destinationRegion.Height)
            };

            var destinationRect = destinationRegion.ToWinRectangle();
            return destinationRect;
        }

        public static Point ToScreenCoordinates(double absoluteX, double absoluteY)
        {
            return ToScreenCoordinates(absoluteX, absoluteY, SystemInformation.VirtualScreen);
        }

        public static Point ToScreenCoordinates(double absoluteX, double absoluteY, Rectangle screenBounds)
        {
            return new Point(
                ToScreenCoordinates(absoluteX, screenBounds.X, screenBounds.Width),
                ToScreenCoordinates(absoluteY, screenBounds.Y, screenBounds.Height));
        }

        public static Point ToScreenCoordinates(PointF winInputCoordinates)
        {
            return ToScreenCoordinates(winInputCoordinates.X, winInputCoordinates.Y);
        }

        public static (double X, double Y) ToWinInputCoordinates(Point screenCoordinates, Rectangle screenBounds)
        {
            return (ToWinInputCoordinates(screenCoordinates.X, screenBounds.X, screenBounds.Width), ToWinInputCoordinates(screenCoordinates.Y, screenBounds.Y, screenBounds.Height));
        }

        public static (double X, double Y) ToWinInputCoordinates(Point screenCoordinates)
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
}