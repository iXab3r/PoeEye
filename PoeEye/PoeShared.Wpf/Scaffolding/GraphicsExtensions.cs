using System.Collections.Generic;
using System.Windows.Media;
using BidirectionalMap;
using WinPixelFormat = System.Drawing.Imaging.PixelFormat;
using WpfPixelFormat = System.Windows.Media.PixelFormat;

namespace PoeShared.Wpf.Scaffolding;

public static class GraphicsExtensions
{
    private static readonly BiMap<WinPixelFormat, WpfPixelFormat> KnownPixelFormats = new(new Dictionary<WinPixelFormat, PixelFormat>()
    {
        { WinPixelFormat.Format24bppRgb, PixelFormats.Bgr24 },
        { WinPixelFormat.Format32bppArgb, PixelFormats.Bgra32 },
        { WinPixelFormat.Format32bppRgb, PixelFormats.Bgr32 },
        { WinPixelFormat.Format32bppPArgb, PixelFormats.Pbgra32 },
    });
        
    public static WinPixelFormat ToWinPixelFormat(this WpfPixelFormat sourceFormat)
    {
        return KnownPixelFormats.Reverse[sourceFormat];
    }
    
    public static WpfPixelFormat ToWpfPixelFormat(this WinPixelFormat sourceFormat)
    {
        return KnownPixelFormats.Forward[sourceFormat];
    }
}