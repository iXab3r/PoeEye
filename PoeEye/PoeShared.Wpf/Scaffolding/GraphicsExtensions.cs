using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using BidirectionalMap;
using WinPixelFormat = System.Drawing.Imaging.PixelFormat;
using WpfPixelFormat = System.Windows.Media.PixelFormat;

namespace PoeShared.Wpf.Scaffolding;

public static class GraphicsExtensions
{
    private static readonly BiMap<WinPixelFormat, WpfPixelFormat> KnownPixelFormats = new(new Dictionary<WinPixelFormat, PixelFormat>()
    {
        {WinPixelFormat.Format24bppRgb, PixelFormats.Bgr24},
        {WinPixelFormat.Format32bppArgb, PixelFormats.Bgra32},
        {WinPixelFormat.Format32bppRgb, PixelFormats.Bgr32},
        {WinPixelFormat.Format32bppPArgb, PixelFormats.Pbgra32},
    });

    public static WinPixelFormat ToWinPixelFormat(this WpfPixelFormat sourceFormat)
    {
        return KnownPixelFormats.Reverse[sourceFormat];
    }

    public static WpfPixelFormat ToWpfPixelFormat(this WinPixelFormat sourceFormat)
    {
        return KnownPixelFormats.Forward[sourceFormat];
    }

    public static Rect TransformToDevice(this Rect rect, Visual visual)
    {
        Matrix matrix = PresentationSource.FromVisual(visual).CompositionTarget.TransformToDevice;
        return Rect.Transform(rect, matrix);
    }

    public static Rect TransformFromDevice(this Rect rect, Visual visual)
    {
        Matrix matrix = PresentationSource.FromVisual(visual).CompositionTarget.TransformFromDevice;
        return Rect.Transform(rect, matrix);
    }

    public static Size TransformToDevice(this Size size, Visual visual)
    {
        Matrix matrix = PresentationSource.FromVisual(visual).CompositionTarget.TransformToDevice;
        return new Size(size.Width * matrix.M11, size.Height * matrix.M22);
    }

    public static Size TransformFromDevice(this Size size, Visual visual)
    {
        Matrix matrix = PresentationSource.FromVisual(visual).CompositionTarget.TransformFromDevice;
        return new Size(size.Width * matrix.M11, size.Height * matrix.M22);
    }

    public static Point TransformToDevice(this Point point, Visual visual)
    {
        Matrix matrix = PresentationSource.FromVisual(visual).CompositionTarget.TransformToDevice;
        return new Point(point.X * matrix.M11, point.Y * matrix.M22);
    }

    public static Point TransformFromDevice(this Point point, Visual visual)
    {
        Matrix matrix = PresentationSource.FromVisual(visual).CompositionTarget.TransformFromDevice;
        return new WpfPoint(point.X * matrix.M11, point.Y * matrix.M22);
    }
}