using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using log4net;
using PInvoke;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.Native
{
    public class ApplicationUtils
    {
        private static readonly IFluentLog Log = typeof(ApplicationUtils).PrepareLogger();

        static ApplicationUtils()
        {
            var appPath = Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(appPath))
            {
                AppIcon = Icon.ExtractAssociatedIcon(appPath);
                AppIconSource = GetAppIcon(new FileInfo(appPath));
            }
        }
        
        public static ImageSource AppIconSource { get; }
        
        public static Icon AppIcon { get; }

        public static ImageSource GetAppIcon(FileInfo path)
        {
            try
            {
                if (!path.Exists)
                {
                    throw new FileNotFoundException("File does not exist", path.FullName);
                }

                var associatedIcon = Icon.ExtractAssociatedIcon(path.FullName);
                if (associatedIcon == null)
                {
                    Log.Warn($"Extracted empty icon from {path}");
                    return null;
                }

                var hbitmap = associatedIcon.ToBitmap().GetHbitmap();
                BitmapSource sourceFromHbitmap;
                try
                {
                    sourceFromHbitmap = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hbitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                }
                finally
                {
                    Gdi32.DeleteObject(hbitmap);
                }
                sourceFromHbitmap.Freeze();
                return sourceFromHbitmap;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to extract icon from {path}", ex);
                return null;
            }
        }
    }
}