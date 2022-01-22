using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PoeShared.Converters;

public sealed class IconToImageSourceConverter : IValueConverter
{
    private readonly ImageSourceConverter imageSourceConverter = new ImageSourceConverter();

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool DeleteObject(IntPtr hObject);
        
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var icon = value as Icon;
        if (icon == null)
        {
            return Binding.DoNothing;
        }
        var result = ToImageSource(icon);
        return result;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
        
    private static ImageSource ToImageSource(Icon icon)
    {
        Bitmap bitmap = icon.ToBitmap();
        IntPtr hBitmap = bitmap.GetHbitmap();

        ImageSource wpfBitmap = Imaging.CreateBitmapSourceFromHBitmap(
            hBitmap,
            IntPtr.Zero,
            Int32Rect.Empty,
            BitmapSizeOptions.FromEmptyOptions());

        if (!DeleteObject(hBitmap))
        {
            throw new Win32Exception();
        }

        return wpfBitmap;
    }
}