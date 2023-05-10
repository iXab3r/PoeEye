using System.Windows;
using System.Windows.Controls;
using PoeShared.Scaffolding;

namespace PoeShared.UI;

public partial class RectangleEditor : UserControl
{
    public static readonly DependencyProperty LabelXProperty = DependencyProperty.Register(
        "LabelX", typeof(string), typeof(RectangleEditor), new PropertyMetadata(default(string)));

    public static readonly DependencyProperty LabelYProperty = DependencyProperty.Register(
        "LabelY", typeof(string), typeof(RectangleEditor), new PropertyMetadata(default(string)));

    public static readonly DependencyProperty LabelWidthProperty = DependencyProperty.Register(
        "LabelWidth", typeof(string), typeof(RectangleEditor), new PropertyMetadata(default(string)));

    public static readonly DependencyProperty LabelHeightProperty = DependencyProperty.Register(
        "LabelHeight", typeof(string), typeof(RectangleEditor), new PropertyMetadata(default(string)));

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        "Value", typeof(ReactiveRectangle), typeof(RectangleEditor), new PropertyMetadata(default(ReactiveRectangle)));

    public RectangleEditor()
    {
        InitializeComponent();
    }

    public ReactiveRectangle Value
    {
        get { return (ReactiveRectangle) GetValue(ValueProperty); }
        set { SetValue(ValueProperty, value); }
    }

    public string LabelX
    {
        get { return (string) GetValue(LabelXProperty); }
        set { SetValue(LabelXProperty, value); }
    }

    public string LabelY
    {
        get { return (string) GetValue(LabelYProperty); }
        set { SetValue(LabelYProperty, value); }
    }

    public string LabelWidth
    {
        get { return (string) GetValue(LabelWidthProperty); }
        set { SetValue(LabelWidthProperty, value); }
    }

    public string LabelHeight
    {
        get { return (string) GetValue(LabelHeightProperty); }
        set { SetValue(LabelHeightProperty, value); }
    }
}