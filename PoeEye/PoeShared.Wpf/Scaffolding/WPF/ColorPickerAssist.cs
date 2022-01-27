using System.Windows;
using System.Windows.Media;

namespace PoeShared.Scaffolding.WPF;

public sealed class ColorPickerAssist
{
    public static readonly DependencyProperty ColorProperty = DependencyProperty.RegisterAttached(
        "Color", typeof(Color), typeof(ColorPickerAssist), new PropertyMetadata(default(Color)));

    public static readonly DependencyProperty PrimaryColorProperty = DependencyProperty.RegisterAttached(
        "PrimaryColor", typeof(Color), typeof(ColorPickerAssist), new PropertyMetadata(default(Color)));

    public static readonly DependencyProperty AlphaChannelProperty = DependencyProperty.RegisterAttached(
        "AlphaChannel", typeof(byte), typeof(ColorPickerAssist), new PropertyMetadata(default(byte)));

    public static void SetColor(DependencyObject element, Color value)
    {
        element.SetValue(ColorProperty, value);
    }

    public static Color GetColor(DependencyObject element)
    {
        return (Color)element.GetValue(ColorProperty);
    }

    public static void SetPrimaryColor(DependencyObject element, Color value)
    {
        element.SetValue(PrimaryColorProperty, value);
    }

    public static Color GetPrimaryColor(DependencyObject element)
    {
        return (Color)element.GetValue(PrimaryColorProperty);
    }

    public static void SetAlphaChannel(DependencyObject element, byte value)
    {
        element.SetValue(AlphaChannelProperty, value);
    }

    public static byte GetAlphaChannel(DependencyObject element)
    {
        return (byte)element.GetValue(AlphaChannelProperty);
    }
}