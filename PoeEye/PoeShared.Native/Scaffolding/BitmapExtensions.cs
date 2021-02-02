using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace PoeShared.Native.Scaffolding
{
    public static class BitmapExtensions
    {
        public static Stream ToStream(this Bitmap bitmap, ImageFormat imageFormat)
        {
            var stream = new MemoryStream();
            bitmap.Save(stream, imageFormat);
            stream.Position = 0;
            return stream;
        }
    }
}