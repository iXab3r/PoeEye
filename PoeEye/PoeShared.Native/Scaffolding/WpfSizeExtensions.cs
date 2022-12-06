using System.Drawing;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace PoeShared.Scaffolding;

public static class WpfSizeExtensions
{
    public static Size Scale(this Size size, double factor)
    {
        return new Size(size.Width * factor, size.Height * factor);
    }
        
    public static Size Scale(this Size size, Point factor)
    {
        return new Size(size.Width * factor.X, size.Height * factor.Y);
    }
    
    public static Size Scale(this Size size, PointF factor)
    {
        return new Size(size.Width * factor.X, size.Height * factor.Y);
    }
        
    public static Size Scale(this Size size, double factorX, double factorY)
    {
        return new Size(size.Width * factorX, size.Height * factorY);
    }
}