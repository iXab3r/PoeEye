using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace PoeShared.Scaffolding;

public static class IconHelper
{
    /// <summary>
    ///     Converts a BMP image to an icon (ICO) file
    /// </summary>
    /// <param name="input">The input stream of BMP image.</param>
    /// <param name="output">The output stream for the ICO file.</param>
    /// <param name="size">Icon size, must be a standard icon dimension (e.g., 16, 32, 48, etc.).</param>
    /// <param name="preserveAspectRatio">Indicates whether the aspect ratio should be preserved.</param>
    /// <exception cref="ArgumentException">Thrown when input parameters are invalid.</exception>
    /// <returns>Whether the icon was successfully generated.</returns>
    public static void ConvertBitmapToIcon(Stream input, Stream output, int size = 64, bool preserveAspectRatio = true)
    {
        var inputBitmap = (Bitmap) Image.FromStream(input);
        
        int width, height;
        if (preserveAspectRatio)
        {
            width = size;
            height = (int) ((float) inputBitmap.Height / inputBitmap.Width * size);
        }
        else
        {
            width = height = size;
        }

        using var newBitmap = new Bitmap(inputBitmap, new WinSize(width, height));
        using var memoryStream = new MemoryStream();
        newBitmap.Save(memoryStream, ImageFormat.Png);
        
        using var iconWriter = new BinaryWriter(output);
        // 0-1 reserved, 0
        iconWriter.Write((byte) 0);
        iconWriter.Write((byte) 0);

        // 2-3 image type, 1 = icon, 2 = cursor
        iconWriter.Write((short) 1);

        // 4-5 number of images
        iconWriter.Write((short) 1);

        // image entry 1
        // 0 image width
        iconWriter.Write((byte) width);
        // 1 image height
        iconWriter.Write((byte) height);

        // 2 number of colors
        iconWriter.Write((byte) 0);

        // 3 reserved
        iconWriter.Write((byte) 0);

        // 4-5 color planes
        iconWriter.Write((short) 0);

        // 6-7 bits per pixel
        iconWriter.Write((short) 32);

        // 8-11 size of image data
        iconWriter.Write((int) memoryStream.Length);

        // 12-15 offset of image data
        iconWriter.Write(6 + 16);

        // write image data
        // png data must contain the whole png data file
        iconWriter.Write(memoryStream.ToArray());
    }
}