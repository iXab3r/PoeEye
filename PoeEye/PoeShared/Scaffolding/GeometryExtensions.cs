namespace PoeShared.Scaffolding;

public static class GeometryExtensions
{
    public static Point ToPoint(this PointF point)
    {
        return new Point((int) point.X, (int) point.Y);
    }
    
    public static PointF ToPointF(this Point point)
    {
        return new PointF(point.X, point.Y);
    }
    
    public static Rectangle ToRectangle(this RectangleF rect)
    {
        return new Rectangle((int) rect.X, (int) rect.Y, (int) rect.Width, (int) rect.Height);
    }
    
    public static RectangleF ToRectangleF(this Rectangle rect)
    {
        return new RectangleF(rect.X, rect.Y, rect.Width, rect.Height);
    }
}