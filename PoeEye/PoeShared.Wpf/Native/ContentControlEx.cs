using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PoeShared.Scaffolding; 
using PoeShared.Logging;

namespace PoeShared.Native;

internal sealed class ContentControlEx : ContentControl
{
    public static readonly DependencyProperty LocationProperty = DependencyProperty.Register(
        "Location", typeof(Point), typeof(ContentControlEx), new PropertyMetadata(default(Point)));

    public Point Location
    {
        get { return (Point) GetValue(LocationProperty); }
        set { SetValue(LocationProperty, value); }
    }

    protected override Size ArrangeOverride(Size arrangeBounds)
    {
        var result = base.ArrangeOverride(arrangeBounds);
        UpdateLocation();
        return result;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        UpdateLocation();
    }

    private void UpdateLocation()
    {
        var owner = this.FindVisualAncestor<Window>();
        Location =  TranslatePoint(new Point(0, 0), owner);
    }
}