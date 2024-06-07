using System.Numerics;

namespace PoeShared.Scaffolding;

public static class VectorExtensions
{
    public static Point ToPoint(this Vector2 vector)
    {
        return new Point((int) vector.X, (int) vector.Y);
    }
    
    public static PointF ToPointF(this Vector2 vector)
    {
        return new PointF(vector.X, vector.Y);
    }
    
    public static Vector2 ToVector2(this Point point)
    {
        return new Vector2(point.X, point.Y);
    }
    
    public static Vector2 ToVector2(this PointF point)
    {
        return new Vector2(point.X, point.Y);
    }
}