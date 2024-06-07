using System.Numerics;

namespace PoeShared.Scaffolding;

public static class VectorExtensions
{
    public static WpfPoint ToWpfPoint(this Vector2 vector)
    {
        return new WpfPoint(vector.X, vector.Y);
    }
    
    public static Vector2 ToVector2(this WpfPoint point)
    {
        return new Vector2((float)point.X, (float)point.Y);
    }
}