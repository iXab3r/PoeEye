using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Imaging;

namespace PoeShared.Scaffolding;

public static class WpfObjectExtensions
{
    /// <summary>
    /// Taken from https://stackoverflow.com/a/27077188/122048 
    /// </summary>
    public static Cursor ConvertToCursor(this UIElement control, WpfPoint hotSpot = default)
    {
        using var pngStream = new MemoryStream();
        control.InvalidateMeasure();
        control.InvalidateArrange();
        control.Measure(new WpfSize(double.PositiveInfinity, double.PositiveInfinity));

        var rect = new WpfRect(0, 0, control.DesiredSize.Width, control.DesiredSize.Height);

        control.Arrange(rect);
        control.UpdateLayout();

        var rtb = control.CaptureScreenshotSimple();
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(rtb));
        encoder.Save(pngStream);

        using var cursorStream = new MemoryStream();
        
        cursorStream.Write(new byte[]
        {
            0x00,
            0x00
        }, 0, 2); // ICONDIR: Reserved. Must always be 0.
        cursorStream.Write(new byte[]
        {
            0x02,
            0x00
        }, 0, 2); // ICONDIR: Specifies image type: 1 for icon (.ICO) image, 2 for cursor (.CUR) image. Other values are invalid
        cursorStream.Write(new byte[]
        {
            0x01,
            0x00
        }, 0, 2); // ICONDIR: Specifies number of images in the file.
        cursorStream.Write(new byte[]
        {
            (byte)control.DesiredSize.Width
        }, 0, 1); // ICONDIRENTRY: Specifies image width in pixels. Can be any number between 0 and 255. Value 0 means image width is 256 pixels.
        cursorStream.Write(new byte[]
        {
            (byte)control.DesiredSize.Height
        }, 0, 1); // ICONDIRENTRY: Specifies image height in pixels. Can be any number between 0 and 255. Value 0 means image height is 256 pixels.
        cursorStream.Write(new byte[]
        {
            0x00
        }, 0, 1); // ICONDIRENTRY: Specifies number of colors in the color palette. Should be 0 if the image does not use a color palette.
        cursorStream.Write(new byte[]
        {
            0x00
        }, 0, 1); // ICONDIRENTRY: Reserved. Should be 0.
        cursorStream.Write(new byte[]
        {
            (byte)hotSpot.X,
            0x00
        }, 0, 2); // ICONDIRENTRY: Specifies the horizontal coordinates of the hotspot in number of pixels from the left.
        cursorStream.Write(new byte[]
        {
            (byte)hotSpot.Y,
            0x00
        }, 0, 2); // ICONDIRENTRY: Specifies the vertical coordinates of the hotspot in number of pixels from the top.
        cursorStream.Write(new byte[]
        {
            // ICONDIRENTRY: Specifies the size of the image's data in bytes
            (byte)(pngStream.Length & 0x000000FF),
            (byte)((pngStream.Length & 0x0000FF00) >> 0x08),
            (byte)((pngStream.Length & 0x00FF0000) >> 0x10),
            (byte)((pngStream.Length & 0xFF000000) >> 0x18)
        }, 0, 4);
        cursorStream.Write(new byte[]
        {
            // ICONDIRENTRY: Specifies the offset of BMP or PNG data from the beginning of the ICO/CUR file
            0x16,
            0x00,
            0x00,
            0x00
        }, 0, 4);

        // copy PNG stream to cursor stream
        pngStream.Seek(0, SeekOrigin.Begin);
        pngStream.CopyTo(cursorStream);

        // return cursor stream
        cursorStream.Seek(0, SeekOrigin.Begin);
        return new Cursor(cursorStream);
    }
    
    public static TItem AddTo<TItem>(this TItem instance, IAddChild parent)
    {
        Guard.ArgumentNotNull(instance, nameof(instance));
        Guard.ArgumentNotNull(parent, nameof(parent));

        parent.AddChild(instance);
        return instance;
    }
    
    public static T AsFrozen<T>(this T freezable) where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
    
    /// <summary>
    /// Forces the value to stay between minimum and maximum.
    /// </summary>
    /// <returns>minimum, if value is less than minimum.
    /// Maximum, if value is greater than maximum.
    /// Otherwise, value.</returns>
    public static double CoerceValue(this double value, double minimum, double maximum)
    {
        return Math.Max(Math.Min(value, maximum), minimum);
    }

    /// <summary>
    /// Forces the value to stay between minimum and maximum.
    /// </summary>
    /// <returns>minimum, if value is less than minimum.
    /// Maximum, if value is greater than maximum.
    /// Otherwise, value.</returns>
    public static int CoerceValue(this int value, int minimum, int maximum)
    {
        return Math.Max(Math.Min(value, maximum), minimum);
    }
}