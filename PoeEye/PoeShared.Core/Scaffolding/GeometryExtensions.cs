using System;
using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using PoeShared.Native;
using Point = System.Windows.Point;
using Size = System.Windows.Size;
using WinSize = System.Drawing.Size;
using WinPoint = System.Drawing.Point;
using WinRectangle = System.Drawing.Rectangle;

namespace PoeShared.Scaffolding
{
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
        public static WinSize FitToSize(this WinRectangle desiredBounds, WinSize sourceSize)
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

        public static bool IsNotEmpty(this Rectangle rect)
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
        
        public static bool IsNotEmpty(this System.Drawing.Size size)
        {
            return size.Width > 0 &&
                   size.Height > 0;
        }

        public static bool IsNotEmpty(this Size size)
        {
            return size.Width > 0 &&
                   size.Height > 0 &&
                   IsFinite(size.Width) &&
                   IsFinite(size.Height);
        }

        public static Size ToWpfSize(this System.Drawing.Size sourceSize)
        {
            return new Size(sourceSize.Width, sourceSize.Height);
        }

        public static System.Drawing.Size ToWinSize(this Size sourceSize)
        {
            return new System.Drawing.Size((int) sourceSize.Width, (int) sourceSize.Height);
        }
        
        public static WinPoint ToScreen(this Point source, Visual owner)
        {
            return owner.PointToScreen(source).ToWinPoint();
        }

        public static Rectangle ToScreen(this Rect sourceSize, Visual owner)
        {
            var topLeft = owner.PointToScreen(sourceSize.TopLeft);
            var bottomRight = owner.PointToScreen(sourceSize.BottomRight);
            var relative = new Rect(topLeft, bottomRight);
            return relative.ToWinRectangle();
        }
        
        public static Rect FromScreen(this Rectangle sourceSize, Visual owner)
        {
            var topLeft = new Point(sourceSize.Left, sourceSize.Top);
            var bottomRight = new Point(sourceSize.Right, sourceSize.Bottom);
            var relative = new Rect(owner.PointFromScreen(topLeft), owner.PointFromScreen(bottomRight));
            return relative;
        }

        public static Rect FromScreen(this Rectangle sourceSize)
        {
            return ToWpfRectangle(sourceSize).ScaleToWpf();
        }

        public static Rect ScaleToScreen(this Rect sourceSize)
        {
            var dpi = UnsafeNative.GetDesktopDpi();

            var result = sourceSize;
            result.Scale(dpi.X, dpi.Y);
            return result;
        }

        public static Rect ScaleToWpf(this Rect sourceSize)
        {
            var dpi = UnsafeNative.GetDesktopDpi();

            var result = sourceSize;
            result.Scale(1 / dpi.X, 1 / dpi.Y);
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

        public static System.Drawing.Point ToWinPoint(this Point source)
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
}