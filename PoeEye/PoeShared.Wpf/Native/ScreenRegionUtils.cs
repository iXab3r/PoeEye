using System;
using System.Drawing;
using PoeShared.Scaffolding;
using WpfRect = System.Windows.Rect;
using WinRect = System.Drawing.Rectangle;
using WinSize = System.Drawing.Size;
using WpfSize = System.Windows.Size;

namespace PoeShared.Native
{
    public static class ScreenRegionUtils
    {
        public static Rectangle CalculateBounds(Rectangle selection,
            Size selectorSize,
            Rectangle clientBounds,
            Rectangle clientRegionBounds)
        {
            if (selection.IsEmpty)
            {
                return Rectangle.Empty;
            }
            
            if (selectorSize.IsEmpty)
            {
                return Rectangle.Empty;
            }

            var destinationSize = selectorSize; // Win Px
            var currentTargetRegion = clientBounds;
            if (!clientRegionBounds.IsEmpty)
            {
                currentTargetRegion.Intersect(clientRegionBounds);
            }
            
            var selectionPercent = new WpfRect
            {
                X = (float)selection.X / destinationSize.Width,
                Y = (float)selection.Y / destinationSize.Height,
                Height = (float)selection.Height / destinationSize.Height,
                Width = (float)selection.Width / destinationSize.Width
            };

            WpfRect currentRegionPercent;
            if (currentTargetRegion.IsNotEmpty())
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
    }
}