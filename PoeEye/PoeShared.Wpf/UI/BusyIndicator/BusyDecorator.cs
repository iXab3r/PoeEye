using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PoeShared.Scaffolding; 
using PoeShared.Logging;

namespace PoeShared.UI;

/// <summary>
/// Use this Decorator to show a busy indicator on top of a child element.
/// </summary>
[StyleTypedProperty(Property = "BusyStyle", StyleTargetType = typeof(Control))]
public sealed class BusyDecorator : Decorator
{
    private Guid backgroundChildId = Guid.Empty;

    /// <summary>
    /// Identifies the IsBusyIndicatorShowing dependency property.
    /// </summary>
    public static readonly DependencyProperty IsBusyIndicatorShowingProperty = DependencyProperty.Register(
        "IsBusyIndicatorShowing",
        typeof(bool),
        typeof(BusyDecorator),
        new FrameworkPropertyMetadata(false,
            FrameworkPropertyMetadataOptions.AffectsMeasure,
            OnIsShowingChanged));

    /// <summary>
    /// Gets or sets if the BusyIndicator is being shown.
    /// </summary>
    public bool IsBusyIndicatorShowing
    {
        get { return (bool)GetValue(IsBusyIndicatorShowingProperty); }
        set { SetValue(IsBusyIndicatorShowingProperty, value); }
    }

    private static void OnIsShowingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var isShowing = (bool)e.NewValue;
        var decorator = (BusyDecorator)d;
        if (isShowing)
        {
            var style = decorator.BusyStyle;
            style?.Seal();

            var horizAlign = decorator.BusyHorizontalAlignment;
            var vertAlign = decorator.BusyVerticalAlignment;
            var margin = decorator.BusyMargin;

            decorator.backgroundChildId = BackgroundVisualHost.AddChild(decorator,
                () => new Control
                {
                    Style = style,
                    HorizontalAlignment = horizAlign,
                    VerticalAlignment = vertAlign,
                    Margin = margin
                });

            if (!decorator.IsEnabledWhenBusy)
            {
                decorator.SetCurrentValue(IsEnabledProperty, false);
            }
        }
        else
        {
            BackgroundVisualHost.RemoveChild(decorator, decorator.backgroundChildId);

            if (!decorator.IsEnabledWhenBusy)
                decorator.SetCurrentValue(IsEnabledProperty, true);
        }
    }

    public static readonly DependencyProperty IsEnabledWhenBusyProperty = DependencyProperty.Register(
        "IsEnabledWhenBusy",
        typeof(bool),
        typeof(BusyDecorator),
        new FrameworkPropertyMetadata(true, HandleIsEnabledWhenBusyChanged));

    public bool IsEnabledWhenBusy
    {
        get { return (bool)GetValue(IsEnabledWhenBusyProperty); }
        set { SetValue(IsEnabledWhenBusyProperty, value); }
    }

    private static void HandleIsEnabledWhenBusyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var decorator = d as BusyDecorator;
        // we must set this if the indicator is showing always. if already busy, and the new 
        // value is true, we want to disable the decorator. if the new value is false, then
        // the decorator has already been disabled, and we need to remove that setting.
        if (decorator.IsBusyIndicatorShowing)
        {
            decorator.SetCurrentValue(IsEnabledProperty, e.NewValue);
        }
    }

    ///<summary>
    /// Identifies the <see cref="BusyStyle" /> property.
    /// </summary>
    public static readonly DependencyProperty BusyStyleProperty =
        DependencyProperty.Register(
            "BusyStyle",
            typeof(Style),
            typeof(BusyDecorator),
            new FrameworkPropertyMetadata(OnBusyStyleChanged));

    /// <summary>
    /// Gets or sets the Style to apply to the Control that is displayed as the busy indication.
    /// </summary>
    public Style BusyStyle
    {
        get { return (Style)GetValue(BusyStyleProperty); }
        set { SetValue(BusyStyleProperty, value); }
    }

    private static void OnBusyStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var bd = (BusyDecorator)d;
        var nVal = e.NewValue as Style;
        nVal?.Seal();
        bd.SetIndicatorProperty(Control.StyleProperty, nVal);
    }

    ///<summary>
    /// Identifies the <see cref="BusyHorizontalAlignment" /> property.
    /// </summary>
    public static readonly DependencyProperty BusyHorizontalAlignmentProperty = DependencyProperty.Register(
        "BusyHorizontalAlignment",
        typeof(HorizontalAlignment),
        typeof(BusyDecorator),
        new FrameworkPropertyMetadata(HorizontalAlignment.Center, OnBusyPropertyChanged));

    /// <summary>
    /// Gets or sets the HorizontalAlignment to use to layout the control that contains the busy indicator control.
    /// </summary>
    public HorizontalAlignment BusyHorizontalAlignment
    {
        get { return (HorizontalAlignment)GetValue(BusyHorizontalAlignmentProperty); }
        set { SetValue(BusyHorizontalAlignmentProperty, value); }
    }

    private static void OnBusyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((BusyDecorator)d).SetIndicatorProperty(e.Property, e.NewValue);
    }

    ///<summary>
    /// Identifies the <see cref="BusyVerticalAlignment" /> property.
    /// </summary>
    public static readonly DependencyProperty BusyVerticalAlignmentProperty = DependencyProperty.Register(
        "BusyVerticalAlignment",
        typeof(VerticalAlignment),
        typeof(BusyDecorator),
        new FrameworkPropertyMetadata(VerticalAlignment.Center, OnBusyPropertyChanged));

    /// <summary>
    /// Gets or sets the the VerticalAlignment to use to layout the control that contains the busy indicator.
    /// </summary>
    public VerticalAlignment BusyVerticalAlignment
    {
        get { return (VerticalAlignment)GetValue(BusyVerticalAlignmentProperty); }
        set { SetValue(BusyVerticalAlignmentProperty, value); }
    }

    public static readonly DependencyProperty BusyMarginProperty = DependencyProperty.Register(
        "BusyMargin",
        typeof(Thickness),
        typeof(BusyDecorator),
        new FrameworkPropertyMetadata(new Thickness(0), OnBusyPropertyChanged));

    public Thickness BusyMargin
    {
        get { return (Thickness)GetValue(BusyMarginProperty); }
        set { SetValue(BusyMarginProperty, value); }
    }

    public static readonly DependencyProperty FadeTimeProperty = DependencyProperty.Register(
        "FadeTime",
        typeof(TimeSpan),
        typeof(BusyDecorator),
        new UIPropertyMetadata(TimeSpan.FromSeconds(.5)));

    /// <summary>
    /// Gets the amount of time that the fade-in/out animation takes
    /// </summary>
    public TimeSpan FadeTime
    {
        get { return (TimeSpan)GetValue(FadeTimeProperty); }
        set { SetValue(FadeTimeProperty, value); }
    }

    static BusyDecorator()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(BusyDecorator),
            new FrameworkPropertyMetadata(typeof(BusyDecorator)));
    }

    public BusyDecorator()
    {
        Loaded += (o, e) => UpdateWindowPosition();
    }

    protected override Size ArrangeOverride(Size arrangeSize)
    {
        if (backgroundChildId != Guid.Empty && IsLoaded)
        {
            // dispatch it so that the arrange pass completes
            Dispatcher.BeginInvoke(new Action(UpdateWindowPosition));
        }
        return base.ArrangeOverride(arrangeSize);
    }

    private void UpdateWindowPosition()
    {
        var root = this.VisualAncestors().OfType<UIElement>().LastOrDefault();
        if (root != null)
        {
            BackgroundVisualHost.WindowPositionChanged(this, this.TranslatePoint(new Point(0, 0), root));
        }
    }

    private void SetIndicatorProperty(DependencyProperty property, object value)
    {
        if (backgroundChildId != Guid.Empty)
            BackgroundVisualHost.DispatchAction(this, backgroundChildId, c => c.SetValue(property, value));
    }
}