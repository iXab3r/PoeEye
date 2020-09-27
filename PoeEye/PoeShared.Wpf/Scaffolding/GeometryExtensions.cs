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

        public static WinSize ToWinSize(this Size sourceSize)
        {
            return new WinSize((int) sourceSize.Width, (int) sourceSize.Height);
        }
        
        public static WinPoint ToScreen(this Point source, Visual owner)
        {
            return owner.PointToScreen(source).ToWinPoint();
        }

        public static Rectangle ToScreen(this Rect sourceSize, Visual owner)
        {
            var ownerTopLeft = owner.PointToScreen(new Point(0, 0));
            var topLeft = owner.PointToScreen(sourceSize.TopLeft);
            topLeft.Offset(-ownerTopLeft.X, -ownerTopLeft.Y);
            var bottomRight = owner.PointToScreen(sourceSize.BottomRight);
            bottomRight.Offset(-ownerTopLeft.X, -ownerTopLeft.Y);
            var relative = new Rect(topLeft, bottomRight);
            return relative.ToWinRectangle();
        }
        
        public static WinSize ToScreen(this Size sourceSize, Visual owner)
        {
            var ownerTopLeft = owner.PointToScreen(new Point(0, 0));
            var bottomRight = owner.PointToScreen(new Point(sourceSize.Width, sourceSize.Height));
            var relative = new Size(bottomRight.X - ownerTopLeft.X, bottomRight.Y - ownerTopLeft.Y);
            return relative.ToWinSize();
        }
        
        public static WinPoint ScaleToScreen(this Point sourceSize)
        {
            var dpi = UnsafeNative.GetDesktopDpi();

            var result = new WinPoint((int)(sourceSize.X * dpi.X), (int)(sourceSize.Y * dpi.Y));
            return result;
        }

        public static WinRectangle ScaleToScreen(this Rect sourceSize)
        {
            return ScaleToScreen(sourceSize, UnsafeNative.GetDesktopWindow());
        }
        
        public static WinRectangle ScaleToScreen(this Rect sourceSize, IntPtr hDesktop)
        {
            if (sourceSize.IsEmpty)
            {
                return WinRectangle.Empty;
            }
            var dpi = UnsafeNative.GetDesktopDpi(hDesktop);
            return ScaleToScreen(sourceSize, dpi);
        }
        
        public static WinRectangle ScaleToScreen(this Rect sourceSize, PointF dpi)
        {
            return new WinRectangle((int)(sourceSize.X * dpi.X), (int)(sourceSize.Y * dpi.Y), (int)(sourceSize.Width * dpi.X), (int)(sourceSize.Height * dpi.Y));
        }
        
        public static WinSize ScaleToScreen(this Size sourceSize)
        {
            var dpi = UnsafeNative.GetDesktopDpi();

            return new WinSize((int)(sourceSize.Width * dpi.X), (int)(sourceSize.Height * dpi.Y));
        }
        
        public static WinSize Scale(this WinSize sourceSize, float dpi)
        {
            return new WinSize((int)(sourceSize.Width / dpi), (int)(sourceSize.Height / dpi));
        }

        public static Rect ScaleToWpf(this WinRectangle sourceSize)
        {
            var dpi = UnsafeNative.GetDesktopDpi();

            var result = sourceSize.ToWpfRectangle();
            result.Scale(1 / dpi.X, 1 / dpi.Y);
            return result;
        }
        
        public static Point ScaleToWpf(this WinPoint source)
        {
            var dpi = UnsafeNative.GetDesktopDpi();
            return new Point(source.X / dpi.X, source.Y / dpi.Y);
        }
        
        public static WinRectangle Scale(this WinRectangle sourceSize, float dpi)
        {
            return new WinRectangle((int)(sourceSize.X / dpi), (int)(sourceSize.Y / dpi), (int)(sourceSize.Width / dpi), (int)(sourceSize.Height / dpi));
        }
        
        public static WinRectangle InflateScale(this WinRectangle sourceSize, float widthMultiplier, float heightMultiplier)
        {
            var result = sourceSize;
            result.Inflate((int)(result.Width * widthMultiplier), (int)(result.Height * heightMultiplier));
            return result;
        }
        
        public static WinRectangle InflateSize(this WinRectangle sourceSize, int width, int height)
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
        
        public static Point ToWpfPoint(this System.Drawing.Point source)
        {
            return new Point
            {
                X = source.X,
                Y = source.Y
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