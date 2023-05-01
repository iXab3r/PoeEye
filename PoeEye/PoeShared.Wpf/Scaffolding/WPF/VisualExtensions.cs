using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GongSolutions.Wpf.DragDrop.Utilities;

namespace PoeShared.Scaffolding.WPF;

public static class VisualExtensions
{
    public static BitmapSource CaptureScreenshot(this Visual target, FlowDirection flowDirection = FlowDirection.LeftToRight)
    {
        if (target == null)
        {
            return null;
        }

        var bounds = VisualTreeHelper.GetDescendantBounds(target);
        var cropBounds = VisualTreeExtensions.GetVisibleDescendantBounds(target);

        var dpiScale = VisualTreeHelper.GetDpi(target);
        var dpiX = dpiScale.PixelsPerInchX;
        var dpiY = dpiScale.PixelsPerInchY;
        var dpiBounds = DpiHelper.LogicalRectToDevice(cropBounds, dpiScale.DpiScaleX, dpiScale.DpiScaleY);

        var pixelWidth = (int)Math.Ceiling(dpiBounds.Width);
        var pixelHeight = (int)Math.Ceiling(dpiBounds.Height);
        if (pixelWidth < 0 || pixelHeight < 0)
        {
            return null;
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
}