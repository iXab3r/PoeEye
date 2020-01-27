using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace PoeShared.Native
{
    public sealed class AspectRatioSizeCalculator
    {
        public Rectangle Calculate(double desiredAspectRatio, Rectangle currentBounds, Rectangle initialBounds, bool prioritizeHeight = true)
        {
            Guard.ArgumentIsTrue(desiredAspectRatio > 0, nameof(desiredAspectRatio));

            if (initialBounds.IsEmpty)
            {
                return currentBounds;
            }

            var result = currentBounds;
            var delta = new
            {
                X = currentBounds.X - initialBounds.X,
                Y = currentBounds.Y - initialBounds.Y,
                Width = currentBounds.Width - initialBounds.Width,
                Height = currentBounds.Height - initialBounds.Height,
            };

            if (delta.X != 0 && delta.Y != 0 && delta.Width != 0 && delta.Height != 0)
            {
                // top-left
                if (prioritizeHeight)
                {
                    var newHeight = GetHeight(currentBounds, desiredAspectRatio);
                    var deltaHeight = newHeight - currentBounds.Height;
                    result.Height = newHeight;
                    result.Y -= deltaHeight;
                }
                else
                {
                    var newWidth = GetWidth(currentBounds, desiredAspectRatio);
                    var deltaWidth = newWidth - currentBounds.Width;
                    result.Width = newWidth;
                    result.X -= deltaWidth;
                }
            } else if (delta.Y != 0 && delta.Width != 0 && delta.Height != 0)
            {
                // top-right
                if (prioritizeHeight)
                {
                    var newHeight = GetHeight(currentBounds, desiredAspectRatio);
                    var deltaHeight = newHeight - currentBounds.Height;
                    result.Height = newHeight;
                    result.Y -= deltaHeight;
                }
                else
                {
                    result.Width = GetWidth(currentBounds, desiredAspectRatio);
                }
            } else if (delta.X != 0 && delta.Width != 0 && delta.Height != 0)
            {
                // bottom-left
                if (prioritizeHeight)
                {
                    result.Height = GetHeight(currentBounds, desiredAspectRatio);
                }
                else
                {
                    var newWidth = GetWidth(currentBounds, desiredAspectRatio);
                    var deltaWidth = newWidth - currentBounds.Width;
                    result.Width = newWidth;
                    result.X -= deltaWidth;
                }
            } else if (delta.Width != 0 && delta.Height != 0)
            {
                // bottom-right
                if (prioritizeHeight)
                {
                    result.Height = GetHeight(currentBounds, desiredAspectRatio);
                }
                else
                {
                    result.Width = GetWidth(currentBounds, desiredAspectRatio);
                }
            } else if (delta.X != 0 || delta.Width != 0)
            {
                result.Height = GetHeight(currentBounds, desiredAspectRatio);
            } else if (delta.Y != 0 || delta.Height != 0)
            {
                result.Width = GetWidth(currentBounds, desiredAspectRatio);
            }
            else if (initialBounds == currentBounds)
            {
                if (prioritizeHeight)
                {
                    result.Height = GetHeight(currentBounds, desiredAspectRatio);
                }
                else
                {
                    result.Width = GetWidth(currentBounds, desiredAspectRatio);
                }
            }
            else
            {
                throw new NotSupportedException($"Unsupported resize technique, delta: {delta}, bounds: {currentBounds}");
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetWidth(Rectangle bounds, double aspectRatio)
        {
            return (int) (bounds.Height * aspectRatio);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetHeight(Rectangle bounds, double aspectRatio)
        {
            return (int) (bounds.Width / aspectRatio);
        }
    }
}