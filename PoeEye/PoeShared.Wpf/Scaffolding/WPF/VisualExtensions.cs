using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GongSolutions.Wpf.DragDrop.Utilities;

namespace PoeShared.Scaffolding.WPF;

public static class VisualExtensions
{
    private static readonly Lazy<MethodInfo> RenderTargetBitmapMethod = new(() => typeof(RenderTargetBitmap)
        .GetMethodOrThrow("RenderForBitmapEffect", BindingFlags.Instance | BindingFlags.NonPublic));
    
    public static BitmapSource CaptureScreenshot(this Visual target, FlowDirection flowDirection = FlowDirection.LeftToRight)
    {
        if (target == null)
        {
            return null;
        }

        var bounds = VisualTreeHelper.GetDescendantBounds(target);
        var cropBounds = VisualTreeExtensions.GetVisibleDescendantBounds(target);
        if (bounds.IsEmptyArea())
        {
            return new BitmapImage();
        }

        var dpiScale = VisualTreeHelper.GetDpi(target);
        var dpiX = dpiScale.PixelsPerInchX;
        var dpiY = dpiScale.PixelsPerInchY;
        var dpiBounds = DpiHelper.LogicalRectToDevice(cropBounds, dpiScale.DpiScaleX, dpiScale.DpiScaleY);

        var pixelWidth = (int)Math.Ceiling(dpiBounds.Width);
        var pixelHeight = (int)Math.Ceiling(dpiBounds.Height);
        if (pixelWidth < 0 || pixelHeight < 0)
        {
            return new BitmapImage();
        }

        var rtb = new RenderTargetBitmap(pixelWidth, pixelHeight, dpiX, dpiY, PixelFormats.Pbgra32);

        var dv = new DrawingVisual();
        using (var ctx = dv.RenderOpen())
        {
            var vb = new VisualBrush(target);

            if (flowDirection == FlowDirection.RightToLeft)
            {
                var transformGroup = new TransformGroup();
                transformGroup.Children.Add(new ScaleTransform(-1, 1));
                transformGroup.Children.Add(new TranslateTransform(bounds.Size.Width, 0));
                ctx.PushTransform(transformGroup);
            }

            ctx.DrawRectangle(vb, null, new WpfRect(new Point(), bounds.Size));
        }

        rtb.Render(dv);

        return rtb;
    }

    public static BitmapSource CaptureScreenshotSimple(this Visual target)
    {
        var bounds = target is FrameworkElement frameworkElement 
            ? new WpfRect(0, 0, frameworkElement.ActualWidth, frameworkElement.ActualHeight) 
            : VisualTreeHelper.GetDescendantBounds(target);
        var dpi = VisualTreeHelper.GetDpi(target);
        var renderTarget = new RenderTargetBitmap(
            (int)(bounds.Width / 96d * dpi.PixelsPerInchX), 
            (int)(bounds.Height / 96d * dpi.PixelsPerInchY), 
            dpi.PixelsPerInchX, 
            dpi.PixelsPerInchY, 
            PixelFormats.Pbgra32);
        renderTarget.Render(target);
        return renderTarget;
    }
    
    public static void CreateBitmapFromVisual(this Visual target, string fileName)
    {
        if (target == null || string.IsNullOrEmpty(fileName))
        {
            return;
        }

        var renderTarget = CaptureScreenshotSimple(target);
        var bitmapEncoder = new PngBitmapEncoder();
        bitmapEncoder.Frames.Add(BitmapFrame.Create(renderTarget));
        using Stream stm = File.Create(fileName);
        bitmapEncoder.Save(stm);
    }
}