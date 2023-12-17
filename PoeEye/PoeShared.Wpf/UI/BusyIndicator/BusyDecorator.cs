using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PoeShared.Logging;
using PoeShared.Prism;
using PoeShared.Scaffolding;

namespace PoeShared.UI;

/// <summary>
/// Use this Decorator to show a busy indicator on top of a child element.
/// </summary>
[StyleTypedProperty(Property = "BusyStyle", StyleTargetType = typeof(Control))]
public sealed class BusyDecorator : Decorator
{
    private static readonly IFluentLog Log = typeof(BusyDecorator).PrepareLogger();

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

    public static readonly DependencyProperty IsEnabledWhenBusyProperty = DependencyProperty.Register(
        "IsEnabledWhenBusy",
        typeof(bool),
        typeof(BusyDecorator),
        new FrameworkPropertyMetadata(true, HandleIsEnabledWhenBusyChanged));

    ///<summary>
    /// Identifies the <see cref="BusyStyle" /> property.
    /// </summary>
    public static readonly DependencyProperty BusyStyleProperty =
        DependencyProperty.Register(
            "BusyStyle",
            typeof(Style),
            typeof(BusyDecorator),
            new FrameworkPropertyMetadata(OnBusyStyleChanged));

    ///<summary>
    /// Identifies the <see cref="BusyHorizontalAlignment" /> property.
    /// </summary>
    public static readonly DependencyProperty BusyHorizontalAlignmentProperty = DependencyProperty.Register(
        "BusyHorizontalAlignment",
        typeof(HorizontalAlignment),
        typeof(BusyDecorator),
        new FrameworkPropertyMetadata(HorizontalAlignment.Center, OnBusyPropertyChanged));

    ///<summary>
    /// Identifies the <see cref="BusyVerticalAlignment" /> property.
    /// </summary>
    public static readonly DependencyProperty BusyVerticalAlignmentProperty = DependencyProperty.Register(
        "BusyVerticalAlignment",
        typeof(VerticalAlignment),
        typeof(BusyDecorator),
        new FrameworkPropertyMetadata(VerticalAlignment.Center, OnBusyPropertyChanged));

    public static readonly DependencyProperty BusyMarginProperty = DependencyProperty.Register(
        "BusyMargin",
        typeof(Thickness),
        typeof(BusyDecorator),
        new FrameworkPropertyMetadata(new Thickness(0), OnBusyPropertyChanged));

    public static readonly DependencyProperty FadeTimeProperty = DependencyProperty.Register(
        "FadeTime",
        typeof(TimeSpan),
        typeof(BusyDecorator),
        new UIPropertyMetadata(TimeSpan.FromSeconds(.5)));

    public static readonly DependencyProperty DataContextFactoryProperty = DependencyProperty.Register(
        nameof(DataContextFactory), typeof(IFactory<object>), typeof(BusyDecorator), new FrameworkPropertyMetadata(default, HandleDataContextFactoryChange));

    private Guid backgroundChildId = Guid.Empty;

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

    /// <summary>
    /// Gets or sets if the BusyIndicator is being shown.
    /// </summary>
    public bool IsBusyIndicatorShowing
    {
        get { return (bool)GetValue(IsBusyIndicatorShowingProperty); }
        set { SetValue(IsBusyIndicatorShowingProperty, value); }
    }

    public bool IsEnabledWhenBusy
    {
        get { return (bool)GetValue(IsEnabledWhenBusyProperty); }
        set { SetValue(IsEnabledWhenBusyProperty, value); }
    }

    /// <summary>
    /// Gets or sets the Style to apply to the Control that is displayed as the busy indication.
    /// </summary>
    public Style BusyStyle
    {
        get { return (Style)GetValue(BusyStyleProperty); }
        set { SetValue(BusyStyleProperty, value); }
    }

    /// <summary>
    /// Gets or sets the HorizontalAlignment to use to layout the control that contains the busy indicator control.
    /// </summary>
    public HorizontalAlignment BusyHorizontalAlignment
    {
        get { return (HorizontalAlignment)GetValue(BusyHorizontalAlignmentProperty); }
        set { SetValue(BusyHorizontalAlignmentProperty, value); }
    }

    /// <summary>
    /// Gets or sets the the VerticalAlignment to use to layout the control that contains the busy indicator.
    /// </summary>
    public VerticalAlignment BusyVerticalAlignment
    {
        get { return (VerticalAlignment)GetValue(BusyVerticalAlignmentProperty); }
        set { SetValue(BusyVerticalAlignmentProperty, value); }
    }

    public Thickness BusyMargin
    {
        get { return (Thickness)GetValue(BusyMarginProperty); }
        set { SetValue(BusyMarginProperty, value); }
    }

    /// <summary>
    /// Gets the amount of time that the fade-in/out animation takes
    /// </summary>
    public TimeSpan FadeTime
    {
        get { return (TimeSpan)GetValue(FadeTimeProperty); }
        set { SetValue(FadeTimeProperty, value); }
    }

    public IFactory<object> DataContextFactory
    {
        get { return (IFactory<object>) GetValue(DataContextFactoryProperty); }
        set { SetValue(DataContextFactoryProperty, value); }
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

            var dataContextFactory = d.GetValue(DataContextFactoryProperty) as IFactory<object>;
            decorator.backgroundChildId = BackgroundVisualHost.AddChild(decorator,
                () =>
                {
                    using var sw = new BenchmarkTimer(Log);
                    sw.Debug(() => $"New child initialization requested, factory: {dataContextFactory}");
                    var childContext = dataContextFactory?.Create();
                    sw.Debug(() => $"New child context created: {childContext}");
                    var element = new CachedContentControl()
                    {
                        Style = style,
                        HorizontalAlignment = horizAlign,
                        VerticalAlignment = vertAlign,
                        Margin = margin,
                        DataContext = childContext
                    };
                    return element;

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
            {
                decorator.SetCurrentValue(IsEnabledProperty, true);
            }
        }
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

    private static void OnBusyStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var decorator = (BusyDecorator)d;
        var newStyle = e.NewValue as Style;
        newStyle?.Seal();
        decorator.SetIndicatorProperty(StyleProperty, newStyle);
    }
    
    private static void HandleDataContextFactoryChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var decorator = (BusyDecorator)d;
        var newDataContextFactory = e.NewValue as IFactory<object>;
        var childId = decorator.backgroundChildId;
        if (childId != Guid.Empty)
        {
            BackgroundVisualHost.DispatchAction(decorator, childId, c => c.SetValue(DataContextProperty, newDataContextFactory?.Create()));
        }
    }
    
    private static void OnBusyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((BusyDecorator)d).SetIndicatorProperty(e.Property, e.NewValue);
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
        {
            BackgroundVisualHost.DispatchAction(this, backgroundChildId, c => c.SetValue(property, value));
        }
    }
}