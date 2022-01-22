using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace PoeShared.UI;

/// <summary>
/// If this AdornerDecorator is used to host Adorners, it will guarantee that the visual created by the BusyDecorator
/// will appear below the Adorners.
/// </summary>
public class BusyAdornerDecorator : AdornerDecorator
{
    internal static readonly DependencyProperty BusyIndicatorHostProperty = DependencyProperty.Register(
        "BusyIndicatorHost",
        typeof(FrameworkElement),
        typeof(BusyAdornerDecorator),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure));

    internal FrameworkElement BusyIndicatorHost
    {
        get { return (FrameworkElement)GetValue(BusyIndicatorHostProperty); }
        set { SetValue(BusyIndicatorHostProperty, value); }
    }

    protected override Size MeasureOverride(Size constraint)
    {
        BusyIndicatorHost?.Measure(constraint);
        return base.MeasureOverride(constraint);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var rect = new Rect(finalSize);
        Child?.Arrange(rect);
        BusyIndicatorHost?.Arrange(rect);

        if (VisualTreeHelper.GetParent(AdornerLayer) != null)
        {
            AdornerLayer.Arrange(rect);
        }

        return finalSize;
    }

    protected override int VisualChildrenCount
    {
        get
        {
            var count = base.VisualChildrenCount;
            if (BusyIndicatorHost != null)
                count++;

            return count;
        }
    }

    protected override Visual GetVisualChild(int index)
    {
        switch (index)
        {
            case 0:
                return Child;

            case 1:
                if (BusyIndicatorHost != null)
                    return BusyIndicatorHost;
                else
                    return AdornerLayer;

            case 2:
                if (BusyIndicatorHost == null)
                    throw new ArgumentOutOfRangeException(nameof(index));
                else
                    return AdornerLayer;

            default:
                throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}