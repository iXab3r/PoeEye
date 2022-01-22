using System.Windows;

namespace PoeShared.Scaffolding;

public static class SizeExtensions
{
    public static Size Scale(this Size size, double factor)
    {
        return new Size(size.Width * factor, size.Height * factor);
    }
        
    public static Size Scale(this Size size, Point factor)
    {
        return new Size(size.Width * factor.X, size.Height * factor.Y);
    }
        
    public static Size Scale(this Size size, double factorX, double factorY)
    {
        return new Size(size.Width * factorX, size.Height * factorY);
    }
}