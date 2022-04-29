using System;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PoeShared.Scaffolding;

namespace PoeShared.UI;

public class ColorPickerEx : MaterialDesignThemes.Wpf.ColorPicker
{
    public static readonly DependencyProperty AlphaProperty = DependencyProperty.Register(
        "Alpha", typeof(byte), typeof(ColorPickerEx), new PropertyMetadata(default(byte)));

    public static readonly DependencyProperty PickerDockProperty = DependencyProperty.Register(
        "PickerDock", typeof(Dock), typeof(ColorPickerEx), new FrameworkPropertyMetadata(default(Dock), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty AllowAlphaProperty = DependencyProperty.Register(
        "AllowAlpha", typeof(bool), typeof(ColorPickerEx), new PropertyMetadata(true));

    public static readonly DependencyProperty ColorWithoutAlphaProperty = DependencyProperty.Register(
        "ColorWithoutAlpha", typeof(Color), typeof(ColorPickerEx), new FrameworkPropertyMetadata(default(Color), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public byte Alpha
    {
        get { return (byte) GetValue(AlphaProperty); }
        set { SetValue(AlphaProperty, value); }
    }

    public bool AllowAlpha
    {
        get { return (bool) GetValue(AllowAlphaProperty); }
        set { SetValue(AllowAlphaProperty, value); }
    }
    public Color ColorWithoutAlpha
    {
        get { return (Color) GetValue(ColorWithoutAlphaProperty); }
        set { SetValue(ColorWithoutAlphaProperty, value); }
    }

    public Dock PickerDock
    {
        get { return (Dock) GetValue(PickerDockProperty); }
        set { SetValue(PickerDockProperty, value); }
    }
    
    static ColorPickerEx()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ColorPickerEx), new FrameworkPropertyMetadata(typeof(ColorPickerEx)));
    }

    public ColorPickerEx()
    {
        var isUpdating = new PauseController();
        Observable.CombineLatest(
                this.Observe(ColorProperty, x => x.Color),
            this.Observe(AllowAlphaProperty, x => x.AllowAlpha),
            (color, allowAlpha) => new { color, allowAlpha }
        )
            .Suspend(isUpdating)
            .Subscribe(x =>
            {
                using var pause = isUpdating.Pause();

                var alpha = x.allowAlpha ? x.color.A : byte.MaxValue;
                this.SetCurrentValueIfChanged(ColorProperty, Color.FromArgb(alpha, x.color.R, x.color.G, x.color.B));
                this.SetCurrentValueIfChanged(ColorWithoutAlphaProperty, Color.FromRgb(x.color.R, x.color.G, x.color.B));
                this.SetCurrentValueIfChanged(AlphaProperty, alpha);
            });
        
        Observable.CombineLatest(
                this.Observe(ColorWithoutAlphaProperty, x => x.ColorWithoutAlpha),
                this.Observe(AlphaProperty, x => x.Alpha),
                this.Observe(AllowAlphaProperty, x => x.AllowAlpha),
                (color, alpha, allowAlpha) => new { color, alpha = allowAlpha ? alpha : byte.MaxValue }
            )
            .Suspend(isUpdating)
            .Subscribe(x =>
            {
                using var pause = isUpdating.Pause();
                this.SetCurrentValueIfChanged(ColorProperty, Color.FromArgb(x.alpha, x.color.R, x.color.G, x.color.B));
            });
    }
}