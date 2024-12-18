using System.Numerics;
using System.Runtime.CompilerServices;

namespace PoeShared.Scaffolding;

public static class VectorExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Point ToPoint(this Vector2 vector)
    {
        return new Point((int) vector.X, (int) vector.Y);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PointF ToPointF(this Vector2 vector)
    {
        return new PointF(vector.X, vector.Y);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 ToVector2(this Point point)
    {
        return new Vector2(point.X, point.Y);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 ToVector2(this PointF point)
    {
        return new Vector2(point.X, point.Y);
    }
    
    /// <summary>
    /// Adds a random offset to the X and Y components of the vector, within the range specified by the maxOffset.
    /// </summary>
    /// <param name="vector">The original vector.</param>
    /// <param name="maxOffset">The maximum random offset for each component.</param>
    /// <returns>A new vector with the random offset applied.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 WithRandomOffset(this Vector2 vector, Vector2 maxOffset)
    {
        var randomOffsetX = maxOffset.X != 0 ? (float)(RandomNumberGenerator.Instance.NextDouble() * 2 - 1) * maxOffset.X : 0;
        var randomOffsetY = maxOffset.Y != 0 ? (float)(RandomNumberGenerator.Instance.NextDouble() * 2 - 1) * maxOffset.Y : 0;
        return new Vector2(vector.X + randomOffsetX, vector.Y + randomOffsetY);
    }
}