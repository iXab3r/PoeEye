using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using ByteSizeLib;
using PoeShared.Logging;
using Point = System.Drawing.Point;

namespace PoeShared.Scaffolding;

public static class BitmapSourceExtensions
{
    private static readonly IFluentLog Log = typeof(BitmapSourceExtensions).PrepareLogger();

    public static Bitmap ToBitmapSafe(this BitmapSource source)
    {
        try
        {
            return source.ToBitmap();
        }
        catch (Exception e)
        {
            Log.Warn($"Failed to convert BitmapSource to bitmap", e);
            return null;
        }
    }
    
    public static Bitmap ToBitmap(this BitmapSource source)
    {
        var bmp = new Bitmap(
            source.PixelWidth,
            source.PixelHeight,
            source.Format.ToWinPixelFormat());

        var data = bmp.LockBits(
            new Rectangle(Point.Empty, bmp.Size),
            ImageLockMode.WriteOnly,
            source.Format.ToWinPixelFormat());
        try
        {
            source.CopyPixels(
                Int32Rect.Empty,
                data.Scan0,
                data.Height * data.Stride,
                data.Stride);
        }
        finally
        {
            bmp.UnlockBits(data);
        }
            
        return bmp;
    }

    [DllImport("gdi32")]
    private static extern int DeleteObject(IntPtr o);

    public static BitmapSource ToBitmapSourceSafe(this byte[] data)
    {
        try
        {
            return data.ToBitmapSource();
        }
        catch (Exception e)
        {
            Log.Warn($"Failed to convert data({ByteSize.FromBytes(data.Length)}) to BitmapSource", e);
            return null;
        }
    }
    
    public static BitmapSource ToBitmapSource(this byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            return null;
        }
        var result = new BitmapImage();
        result.BeginInit();
        result.StreamSource = new MemoryStream(data);
        result.EndInit();
        return result;
    }
        
    public static BitmapSource ToBitmapSource(this Bitmap bitmap)
    {
        Guard.ArgumentNotNull(bitmap, nameof(bitmap));
            
        if (bitmap.Width <= 0 || bitmap.Height <= 0)
        {
            throw new ApplicationException($"Invalid bitmap width/height, {bitmap.Width}x{bitmap.Height}");
        }

        if (bitmap.VerticalResolution <= 0 || bitmap.HorizontalResolution <= 0)
        {
            throw new ApplicationException($"Invalid bitmap horizontal/vertical resolution, {bitmap.HorizontalResolution}x{bitmap.VerticalResolution}");
        }
            
        lock (bitmap)
        {
            var hBitmap = bitmap.GetHbitmap();
            try
            {
                var result = Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                result.Freeze();
                return result;
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }
    }
}