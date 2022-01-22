using System.Windows;
using System.Windows.Controls;

namespace PoeShared.UI;

public partial class ButtonEx : Button
{
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
        "Icon", typeof(object), typeof(ButtonEx), new PropertyMetadata(default(object)));

    public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
        "CornerRadius", typeof(CornerRadius), typeof(ButtonEx), new PropertyMetadata(default(CornerRadius)));

    public object Icon
    {
        get { return (object) GetValue(IconProperty); }
        set { SetValue(IconProperty, value); }
    }
    public CornerRadius CornerRadius
    {
        get { return (CornerRadius) GetValue(CornerRadiusProperty); }
        set { SetValue(CornerRadiusProperty, value); }
    }
}