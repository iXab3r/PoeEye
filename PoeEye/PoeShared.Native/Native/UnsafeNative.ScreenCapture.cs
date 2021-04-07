using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using WindowsFormsAero;
using PInvoke;
using PoeShared.Native.Native;
using PoeShared.Scaffolding;

namespace PoeShared.Native
{
    public partial class UnsafeNative
    {
        private const int OBJ_BITMAP = 7;

        private const int PW_RENDERFULLCONTENT = 0x00000002;

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern IntPtr GetCurrentObject(IntPtr hdc, ushort objectType);


        public static Bitmap GetWindowImageViaPrintWindow(IntPtr hwnd)
        {
            return GetWindowImageViaPrintWindow(hwnd, Rectangle.Empty);
        }

        public static Bitmap GetDesktopImageViaCopyFromScreen(Rectangle region)
        {
            if (region.Width <= 0 || region.Height <= 0)
            {
                return null;
            }
            
            var sourceBmp = new Bitmap(region.Width, region.Height, PixelFormat.Format32bppRgb); // Format32bppRgb is used because in most cases PrintWindow/CopyDeviceContext returns 32bppRgb
            using var captureGraphics = Graphics.FromImage(sourceBmp);
            captureGraphics.CopyFromScreen(region.Left,region.Top,0,0,region.Size);
            return sourceBmp;
        }
        
        public static Bitmap GetWindowImageViaCopyFromScreen(IntPtr hwnd, Rectangle region)
        {
            var sourceRegion = GetAbsoluteClientRect(hwnd);
            if (sourceRegion.Width <= 0 || sourceRegion.Height <= 0)
            {
                return null;
            }
                
            var captureRegion = sourceRegion;
            if (!region.IsEmpty)
            {
                captureRegion.Intersect(region);
            }

            return GetDesktopImageViaCopyFromScreen(captureRegion);
        }

        public static Bitmap GetWindowImageViaPrintWindow(IntPtr hwnd, Rectangle region)
        {
            if (hwnd == IntPtr.Zero)
            {
                throw new ArgumentException("Window handle must be set");
            }

            try
            {
                var sourceRegion = GetAbsoluteClientRect(hwnd);
                if (sourceRegion.Width <= 0 || sourceRegion.Height <= 0)
                {
                    return null;
                }

                Bitmap sourceBmp = null;
                using var hdc = User32.GetWindowDC(hwnd);
                if (hdc.IsInvalid || hdc.IsClosed)
                {
                    throw new InvalidOperationException($"Failed to create HDC for HWND {hwnd.ToHexadecimal()} (title: {GetWindowTitle(hwnd)}, rect: {GetWindowRect(hwnd)})");
                }

                using var hSourceBmp = new SafeGdiHandle(Gdi32.CreateCompatibleBitmap(hdc, sourceRegion.Width, sourceRegion.Height));
                if (hSourceBmp.IsInvalid || hSourceBmp.IsClosed)
                {
                    throw new InvalidOperationException($"Failed to create Compatible BMP for HWND {hwnd.ToHexadecimal()} (title: {GetWindowTitle(hwnd)}, rect: {GetWindowRect(hwnd)})");
                }

                using var hMemDC = Gdi32.CreateCompatibleDC(hdc); // MUST be DeleteDC'ed before disposal
                if (hMemDC.IsInvalid || hMemDC.IsClosed)
                {
                    throw new InvalidOperationException($"Failed to create Compatible DC for HWND {hwnd.ToHexadecimal()} (title: {GetWindowTitle(hwnd)}, rect: {GetWindowRect(hwnd)})");
                }

                Gdi32.SelectObject(hMemDC, hSourceBmp.DangerousGetHandle());

                try
                {
                    if (OsSupport.IsCompositionEnabled)
                    {
                        var bitBltResult = Gdi32.BitBlt(hMemDC.DangerousGetHandle(), 0, 0, sourceRegion.Width, sourceRegion.Height,
                            hdc.DangerousGetHandle(), 0,
                            0, (int) (TernaryRasterOperations.SRCCOPY | TernaryRasterOperations.CAPTUREBLT));
                        var lastError = Kernel32.GetLastError();
                        if (!bitBltResult || lastError != Win32ErrorCode.NERR_Success)
                        {
                            Log.Warn($"Failed to BitBlt content of HWND {hwnd.ToHexadecimal()} (title: {GetWindowTitle(hwnd)}, rect: {GetWindowRect(hwnd)}), error: {lastError}");
                        }
                        else
                        {
                            sourceBmp = Image.FromHbitmap(hSourceBmp.DangerousGetHandle());
                        }
                    }

                    if (sourceBmp == null)
                    {
                        if (!User32.PrintWindow(hwnd,
                            hMemDC.DangerousGetHandle(),
                            (User32.PrintWindowFlags) PW_RENDERFULLCONTENT))
                        {
                            Log.Warn($"Failed to PrintWindow content of HWND {hwnd.ToHexadecimal()} (title: {GetWindowTitle(hwnd)}, rect: {GetWindowRect(hwnd)})");
                        }
                        else
                        {
                            sourceBmp = Image.FromHbitmap(hSourceBmp.DangerousGetHandle());
                        }
                    }

                    if (sourceBmp == null)
                    {
                        Log.Warn($"Failed to capture content using any known methods,  HWND {hwnd.ToHexadecimal()} (title: {GetWindowTitle(hwnd)}, rect: {GetWindowRect(hwnd)})");
                    }
                }
                finally
                {
                    Gdi32.DeleteDC(hMemDC);
                }

                if (sourceBmp == null || region.IsEmpty)
                {
                    return sourceBmp;
                }

                try
                {
                    var sourceBmpRegion = new Rectangle(0, 0, sourceBmp.Width, sourceBmp.Height);
                    sourceBmpRegion.Intersect(region);

                    var result = sourceBmp.Clone(sourceBmpRegion, sourceBmp.PixelFormat);
                    return result;
                }
                finally
                {
                    sourceBmp.Dispose();
                }
            }
            catch (Exception e)
            {
                Log.HandleException(e);
            }

            return null;
        }

        public static Rectangle GetAbsoluteClientRect(IntPtr hWnd)
        {
            var windowRect = GetWindowRect(hWnd);
            var clientRect = GetClientRect(hWnd);

            // This gives us the width of the left, right and bottom chrome - we can then determine the top height
            var chromeWidth = (windowRect.Width - clientRect.Width) / 2;

            return new Rectangle(new Point(windowRect.X + chromeWidth, windowRect.Y + (windowRect.Height - clientRect.Height - chromeWidth)), clientRect.Size);
        }

        public static Bitmap GetWindowImageViaDeviceContext(IntPtr hwnd)
        {
            using var desktopDC = User32.GetDC(hwnd);
            if (desktopDC == null || desktopDC.IsInvalid || desktopDC.IsClosed)
            {
                throw new InvalidOperationException($"Failed to get DC of HWND {hwnd.ToHexadecimal()} (title: {GetWindowTitle(hwnd)}, rect: {GetWindowRect(hwnd)})");
            }

            using var desktopBitmap = new User32.SafeDesktopHandle(GetCurrentObject(desktopDC.DangerousGetHandle(), OBJ_BITMAP));
            if (desktopBitmap == null || desktopBitmap.IsInvalid || desktopBitmap.IsClosed)
            {
                throw new InvalidOperationException($"Failed to get Bitmap of HWND {hwnd.ToHexadecimal()} (title: {GetWindowTitle(hwnd)}, rect: {GetWindowRect(hwnd)})");
            }

            var desktopImage = Image.FromHbitmap(desktopBitmap.DangerousGetHandle());
            return desktopImage;
        }

        [Flags]
        private enum TernaryRasterOperations : uint
        {
            SRCCOPY = 0x00CC0020,
            SRCPAINT = 0x00EE0086,
            SRCAND = 0x008800C6,
            SRCINVERT = 0x00660046,
            SRCERASE = 0x00440328,
            NOTSRCCOPY = 0x00330008,
            NOTSRCERASE = 0x001100A6,
            MERGECOPY = 0x00C000CA,
            MERGEPAINT = 0x00BB0226,
            PATCOPY = 0x00F00021,
            PATPAINT = 0x00FB0A09,
            PATINVERT = 0x005A0049,
            DSTINVERT = 0x00550009,
            BLACKNESS = 0x00000042,
            WHITENESS = 0x00FF0062,
            CAPTUREBLT = 0x40000000 //only if WinVer >= 5.0.0 (see wingdi.h)
        }
    }
}