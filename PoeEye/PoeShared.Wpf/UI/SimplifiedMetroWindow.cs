using System.Diagnostics;
using System.Linq;
using ControlzEx.Native;
using MahApps.Metro.Controls;
using PInvoke;
using PoeShared.Scaffolding;

namespace PoeShared.UI;

using System;
using System.Collections;
using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Controls.Primitives;
using ControlzEx;
using ControlzEx.Theming;
using MahApps.Metro.Automation.Peers;
using MahApps.Metro.Behaviors;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.ValueBoxes;
using Microsoft.Xaml.Behaviors;

/// <summary>
/// An extended Window class.
/// </summary>
[TemplatePart(Name = PART_Icon, Type = typeof(UIElement))]
[TemplatePart(Name = PART_TitleBar, Type = typeof(UIElement))]
[TemplatePart(Name = PART_WindowTitleBackground, Type = typeof(UIElement))]
[TemplatePart(Name = PART_WindowTitleThumb, Type = typeof(Thumb))]
[TemplatePart(Name = PART_LeftWindowCommands, Type = typeof(ContentPresenter))]
[TemplatePart(Name = PART_RightWindowCommands, Type = typeof(ContentPresenter))]
[TemplatePart(Name = PART_WindowButtonCommands, Type = typeof(ContentPresenter))]
[TemplatePart(Name = PART_Content, Type = typeof(MetroContentControl))]
public class MetroWindow : WindowChromeWindow
{
    private static readonly DependencyPropertyKey ParentWindowPropertyKey;
    
    private const string PART_Icon = "PART_Icon";
    private const string PART_TitleBar = "PART_TitleBar";
    private const string PART_WindowTitleBackground = "PART_WindowTitleBackground";
    private const string PART_WindowTitleThumb = "PART_WindowTitleThumb";
    private const string PART_LeftWindowCommands = "PART_LeftWindowCommands";
    private const string PART_RightWindowCommands = "PART_RightWindowCommands";
    private const string PART_WindowButtonCommands = "PART_WindowButtonCommands";
    private const string PART_Content = "PART_Content";

    private FrameworkElement icon;
    private UIElement titleBar;
    private UIElement titleBarBackground;
    private Thumb windowTitleThumb;
    private ContentPresenter LeftWindowCommandsPresenter;
    private ContentPresenter RightWindowCommandsPresenter;
    private ContentPresenter WindowButtonCommandsPresenter;

    private EventHandler onOverlayFadeInStoryboardCompleted = null;
    private EventHandler onOverlayFadeOutStoryboardCompleted = null;

    /// <summary>Identifies the <see cref="ShowIconOnTitleBar"/> dependency property.</summary>
    public static readonly DependencyProperty ShowIconOnTitleBarProperty
        = DependencyProperty.Register(nameof(ShowIconOnTitleBar),
            typeof(bool),
            typeof(MetroWindow),
            new PropertyMetadata(BooleanBoxes.TrueBox, OnShowIconOnTitleBarPropertyChangedCallback));

    private static void OnShowIconOnTitleBarPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var window = (MetroWindow) d;
        if (e.NewValue != e.OldValue)
        {
            window.UpdateIconVisibility();
        }
    }

    /// <summary>
    /// Get or sets whether the TitleBar icon is visible or not.
    /// </summary>
    public bool ShowIconOnTitleBar
    {
        get => (bool) GetValue(ShowIconOnTitleBarProperty);
        set => SetValue(ShowIconOnTitleBarProperty, BooleanBoxes.Box(value));
    }

    /// <summary>Identifies the <see cref="IconEdgeMode"/> dependency property.</summary>
    public static readonly DependencyProperty IconEdgeModeProperty
        = DependencyProperty.Register(nameof(IconEdgeMode),
            typeof(EdgeMode),
            typeof(MetroWindow),
            new PropertyMetadata(EdgeMode.Aliased));

    /// <summary>
    /// Gets or sets the edge mode for the TitleBar icon.
    /// </summary>
    public EdgeMode IconEdgeMode
    {
        get => (EdgeMode) GetValue(IconEdgeModeProperty);
        set => SetValue(IconEdgeModeProperty, value);
    }

    /// <summary>Identifies the <see cref="IconBitmapScalingMode"/> dependency property.</summary>
    public static readonly DependencyProperty IconBitmapScalingModeProperty
        = DependencyProperty.Register(nameof(IconBitmapScalingMode),
            typeof(BitmapScalingMode),
            typeof(MetroWindow),
            new PropertyMetadata(BitmapScalingMode.HighQuality));

    /// <summary>
    /// Gets or sets the bitmap scaling mode for the TitleBar icon.
    /// </summary>
    public BitmapScalingMode IconBitmapScalingMode
    {
        get => (BitmapScalingMode) GetValue(IconBitmapScalingModeProperty);
        set => SetValue(IconBitmapScalingModeProperty, value);
    }

    /// <summary>Identifies the <see cref="IconScalingMode"/> dependency property.</summary>
    public static readonly DependencyProperty IconScalingModeProperty
        = DependencyProperty.Register(nameof(IconScalingMode),
            typeof(MultiFrameImageMode),
            typeof(MetroWindow),
            new FrameworkPropertyMetadata(MultiFrameImageMode.ScaleDownLargerFrame, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>
    /// Gets or sets the scaling mode for the TitleBar icon.
    /// </summary>
    public MultiFrameImageMode IconScalingMode
    {
        get => (MultiFrameImageMode) GetValue(IconScalingModeProperty);
        set => SetValue(IconScalingModeProperty, value);
    }

    /// <summary>Identifies the <see cref="CloseOnIconDoubleClick"/> dependency property.</summary>
    public static readonly DependencyProperty CloseOnIconDoubleClickProperty
        = DependencyProperty.Register(nameof(CloseOnIconDoubleClick),
            typeof(bool),
            typeof(MetroWindow),
            new PropertyMetadata(BooleanBoxes.TrueBox));

    /// <summary>
    /// Gets or sets the value to close the window if the user double click on the window icon.
    /// </summary>
    public bool CloseOnIconDoubleClick
    {
        get => (bool) GetValue(CloseOnIconDoubleClickProperty);
        set => SetValue(CloseOnIconDoubleClickProperty, BooleanBoxes.Box(value));
    }

    /// <summary>Identifies the <see cref="ShowTitleBar"/> dependency property.</summary>
    public static readonly DependencyProperty ShowTitleBarProperty
        = DependencyProperty.Register(nameof(ShowTitleBar),
            typeof(bool),
            typeof(MetroWindow),
            new PropertyMetadata(BooleanBoxes.TrueBox, OnShowTitleBarPropertyChangedCallback));

    /// <summary>
    /// Gets or sets whether the TitleBar is visible or not.
    /// </summary>
    public bool ShowTitleBar
    {
        get => (bool) GetValue(ShowTitleBarProperty);
        set => SetValue(ShowTitleBarProperty, BooleanBoxes.Box(value));
    }

    /// <summary>Identifies the <see cref="ShowCloseButton"/> dependency property.</summary>
    public static readonly DependencyProperty ShowCloseButtonProperty
        = DependencyProperty.Register(nameof(ShowCloseButton),
            typeof(bool),
            typeof(MetroWindow),
            new PropertyMetadata(BooleanBoxes.TrueBox));

    /// <summary>
    /// Gets or sets whether if the close button is visible.
    /// </summary>
    public bool ShowCloseButton
    {
        get => (bool) GetValue(ShowCloseButtonProperty);
        set => SetValue(ShowCloseButtonProperty, BooleanBoxes.Box(value));
    }

    /// <summary>Identifies the <see cref="IsMinButtonEnabled"/> dependency property.</summary>
    public static readonly DependencyProperty IsMinButtonEnabledProperty
        = DependencyProperty.Register(nameof(IsMinButtonEnabled),
            typeof(bool),
            typeof(MetroWindow),
            new PropertyMetadata(BooleanBoxes.TrueBox));

    /// <summary>
    /// Gets or sets if the minimize button is enabled.
    /// </summary>
    public bool IsMinButtonEnabled
    {
        get => (bool) GetValue(IsMinButtonEnabledProperty);
        set => SetValue(IsMinButtonEnabledProperty, BooleanBoxes.Box(value));
    }

    /// <summary>Identifies the <see cref="IsMaxRestoreButtonEnabled"/> dependency property.</summary>
    public static readonly DependencyProperty IsMaxRestoreButtonEnabledProperty
        = DependencyProperty.Register(nameof(IsMaxRestoreButtonEnabled),
            typeof(bool),
            typeof(MetroWindow),
            new PropertyMetadata(BooleanBoxes.TrueBox));

    /// <summary>
    /// Gets or sets if the maximize/restore button is enabled.
    /// </summary>
    public bool IsMaxRestoreButtonEnabled
    {
        get => (bool) GetValue(IsMaxRestoreButtonEnabledProperty);
        set => SetValue(IsMaxRestoreButtonEnabledProperty, BooleanBoxes.Box(value));
    }

    /// <summary>Identifies the <see cref="IsCloseButtonEnabled"/> dependency property.</summary>
    public static readonly DependencyProperty IsCloseButtonEnabledProperty
        = DependencyProperty.Register(nameof(IsCloseButtonEnabled),
            typeof(bool),
            typeof(MetroWindow),
            new PropertyMetadata(BooleanBoxes.TrueBox));

    /// <summary>
    /// Gets or sets if the close button is enabled.
    /// </summary>
    public bool IsCloseButtonEnabled
    {
        get => (bool) GetValue(IsCloseButtonEnabledProperty);
        set => SetValue(IsCloseButtonEnabledProperty, BooleanBoxes.Box(value));
    }

    /// <summary>Identifies the <see cref="ShowSystemMenu"/> dependency property.</summary>
    public static readonly DependencyProperty ShowSystemMenuProperty
        = DependencyProperty.Register(nameof(ShowSystemMenu),
            typeof(bool),
            typeof(MetroWindow),
            new PropertyMetadata(BooleanBoxes.TrueBox));

    /// <summary>
    /// Gets or sets a value that indicates whether the system menu should popup with left mouse click on the window icon.
    /// </summary>
    public bool ShowSystemMenu
    {
        get => (bool) GetValue(ShowSystemMenuProperty);
        set => SetValue(ShowSystemMenuProperty, BooleanBoxes.Box(value));
    }

    /// <summary>Identifies the <see cref="ShowSystemMenuOnRightClick"/> dependency property.</summary>
    public static readonly DependencyProperty ShowSystemMenuOnRightClickProperty
        = DependencyProperty.Register(nameof(ShowSystemMenuOnRightClick),
            typeof(bool),
            typeof(MetroWindow),
            new PropertyMetadata(BooleanBoxes.TrueBox));

    /// <summary>
    /// Gets or sets a value that indicates whether the system menu should popup with right mouse click if the mouse position is on title bar or on the entire window if it has no TitleBar (and no TitleBar height).
    /// </summary>
    public bool ShowSystemMenuOnRightClick
    {
        get => (bool) GetValue(ShowSystemMenuOnRightClickProperty);
        set => SetValue(ShowSystemMenuOnRightClickProperty, BooleanBoxes.Box(value));
    }

    /// <summary>Identifies the <see cref="TitleBarHeight"/> dependency property.</summary>
    public static readonly DependencyProperty TitleBarHeightProperty
        = DependencyProperty.Register(nameof(TitleBarHeight),
            typeof(int),
            typeof(MetroWindow),
            new PropertyMetadata(30, TitleBarHeightPropertyChangedCallback));

    private static void TitleBarHeightPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue != e.OldValue)
        {
            ((MetroWindow) d).UpdateTitleBarElementsVisibility();
        }
    }

    /// <summary>
    /// Gets or sets the TitleBar's height.
    /// </summary>
    public int TitleBarHeight
    {
        get => (int) GetValue(TitleBarHeightProperty);
        set => SetValue(TitleBarHeightProperty, value);
    }

    /// <summary>Identifies the <see cref="TitleCharacterCasing"/> dependency property.</summary>
    public static readonly DependencyProperty TitleCharacterCasingProperty
        = DependencyProperty.Register(nameof(TitleCharacterCasing),
            typeof(CharacterCasing),
            typeof(MetroWindow),
            new FrameworkPropertyMetadata(CharacterCasing.Upper, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsMeasure),
            value => CharacterCasing.Normal <= (CharacterCasing) value && (CharacterCasing) value <= CharacterCasing.Upper);

    /// <summary>
    /// Gets or sets the Character casing of the title.
    /// </summary>
    public CharacterCasing TitleCharacterCasing
    {
        get => (CharacterCasing) GetValue(TitleCharacterCasingProperty);
        set => SetValue(TitleCharacterCasingProperty, value);
    }

    /// <summary>Identifies the <see cref="TitleAlignment"/> dependency property.</summary>
    public static readonly DependencyProperty TitleAlignmentProperty
        = DependencyProperty.Register(nameof(TitleAlignment),
            typeof(HorizontalAlignment),
            typeof(MetroWindow),
            new PropertyMetadata(HorizontalAlignment.Stretch, OnTitleAlignmentChanged));

    private static void OnTitleAlignmentChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue != e.NewValue)
        {
            var window = (MetroWindow) dependencyObject;

            window.SizeChanged -= window.MetroWindow_SizeChanged;
            if (e.NewValue is HorizontalAlignment horizontalAlignment && horizontalAlignment == HorizontalAlignment.Center && window.titleBar != null)
            {
                window.SizeChanged += window.MetroWindow_SizeChanged;
            }
        }
    }

    /// <summary>
    /// Gets or sets the horizontal alignment of the title.
    /// </summary>
    public HorizontalAlignment TitleAlignment
    {
        get => (HorizontalAlignment) GetValue(TitleAlignmentProperty);
        set => SetValue(TitleAlignmentProperty, value);
    }

    /// <summary>Identifies the <see cref="SaveWindowPosition"/> dependency property.</summary>
    public static readonly DependencyProperty SaveWindowPositionProperty
        = DependencyProperty.Register(nameof(SaveWindowPosition),
            typeof(bool),
            typeof(MetroWindow),
            new PropertyMetadata(BooleanBoxes.FalseBox));

    /// <summary>
    /// Gets or sets whether the window will save it's position and size.
    /// </summary>
    public bool SaveWindowPosition
    {
        get => (bool) GetValue(SaveWindowPositionProperty);
        set => SetValue(SaveWindowPositionProperty, BooleanBoxes.Box(value));
    }

    /// <summary>Identifies the <see cref="WindowPlacementSettings"/> dependency property.</summary>
    public static readonly DependencyProperty WindowPlacementSettingsProperty
        = DependencyProperty.Register(nameof(WindowPlacementSettings),
            typeof(IWindowPlacementSettings),
            typeof(MetroWindow),
            new PropertyMetadata(null));

    /// <summary>
    ///  Gets or sets the settings to save and load the position and size of the window.
    /// </summary>
    public IWindowPlacementSettings WindowPlacementSettings
    {
        get => (IWindowPlacementSettings) GetValue(WindowPlacementSettingsProperty);
        set => SetValue(WindowPlacementSettingsProperty, value);
    }

    /// <summary>Identifies the <see cref="TitleForeground"/> dependency property.</summary>
    public static readonly DependencyProperty TitleForegroundProperty
        = DependencyProperty.Register(nameof(TitleForeground),
            typeof(Brush),
            typeof(MetroWindow));

    /// <summary>
    /// Gets or sets the brush used for the TitleBar's foreground.
    /// </summary>
    public Brush TitleForeground
    {
        get => (Brush) GetValue(TitleForegroundProperty);
        set => SetValue(TitleForegroundProperty, value);
    }

    /// <summary>Identifies the <see cref="TitleTemplate"/> dependency property.</summary>
    public static readonly DependencyProperty TitleTemplateProperty
        = DependencyProperty.Register(nameof(TitleTemplate),
            typeof(DataTemplate),
            typeof(MetroWindow),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the <see cref="DataTemplate"/> for the <see cref="Window.Title"/>.
    /// </summary>
    public DataTemplate TitleTemplate
    {
        get => (DataTemplate) GetValue(TitleTemplateProperty);
        set => SetValue(TitleTemplateProperty, value);
    }

    /// <summary>Identifies the <see cref="WindowTitleBrush"/> dependency property.</summary>
    public static readonly DependencyProperty WindowTitleBrushProperty
        = DependencyProperty.Register(nameof(WindowTitleBrush),
            typeof(Brush),
            typeof(MetroWindow),
            new PropertyMetadata(Brushes.Transparent));

    /// <summary>
    /// Gets or sets the brush used for the background of the TitleBar.
    /// </summary>
    public Brush WindowTitleBrush
    {
        get => (Brush) GetValue(WindowTitleBrushProperty);
        set => SetValue(WindowTitleBrushProperty, value);
    }

    /// <summary>Identifies the <see cref="NonActiveWindowTitleBrush"/> dependency property.</summary>
    public static readonly DependencyProperty NonActiveWindowTitleBrushProperty
        = DependencyProperty.Register(nameof(NonActiveWindowTitleBrush),
            typeof(Brush),
            typeof(MetroWindow),
            new PropertyMetadata(Brushes.Gray));

    /// <summary>
    /// Gets or sets the non-active brush used for the background of the TitleBar.
    /// </summary>
    public Brush NonActiveWindowTitleBrush
    {
        get => (Brush) GetValue(NonActiveWindowTitleBrushProperty);
        set => SetValue(NonActiveWindowTitleBrushProperty, value);
    }

    /// <summary>Identifies the <see cref="NonActiveBorderBrush"/> dependency property.</summary>
    public static readonly DependencyProperty NonActiveBorderBrushProperty
        = DependencyProperty.Register(nameof(NonActiveBorderBrush),
            typeof(Brush),
            typeof(MetroWindow),
            new PropertyMetadata(Brushes.Gray));

    /// <summary>
    /// Gets or sets the non-active brush used for the border of the window.
    /// </summary>
    public Brush NonActiveBorderBrush
    {
        get => (Brush) GetValue(NonActiveBorderBrushProperty);
        set => SetValue(NonActiveBorderBrushProperty, value);
    }

    /// <summary>Identifies the <see cref="OverlayBrush"/> dependency property.</summary>
    public static readonly DependencyProperty OverlayBrushProperty
        = DependencyProperty.Register(nameof(OverlayBrush),
            typeof(Brush),
            typeof(MetroWindow),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the brush used for the overlay when a dialog is open.
    /// </summary>
    public Brush OverlayBrush
    {
        get => (Brush) GetValue(OverlayBrushProperty);
        set => SetValue(OverlayBrushProperty, value);
    }

    /// <summary>Identifies the <see cref="OverlayOpacity"/> dependency property.</summary>
    public static readonly DependencyProperty OverlayOpacityProperty
        = DependencyProperty.Register(nameof(OverlayOpacity),
            typeof(double),
            typeof(MetroWindow),
            new PropertyMetadata(0.7d));

    /// <summary>
    /// Gets or sets the opacity used for the overlay when a dialog is open.
    /// </summary>
    public double OverlayOpacity
    {
        get => (double) GetValue(OverlayOpacityProperty);
        set => SetValue(OverlayOpacityProperty, value);
    }

    /// <summary>Identifies the <see cref="OverlayFadeIn"/> dependency property.</summary>
    public static readonly DependencyProperty OverlayFadeInProperty
        = DependencyProperty.Register(nameof(OverlayFadeIn),
            typeof(Storyboard),
            typeof(MetroWindow),
            new PropertyMetadata(default(Storyboard)));

    /// <summary>
    /// Gets or sets the storyboard for the overlay fade in effect.
    /// </summary>
    public Storyboard OverlayFadeIn
    {
        get => (Storyboard) GetValue(OverlayFadeInProperty);
        set => SetValue(OverlayFadeInProperty, value);
    }

    /// <summary>Identifies the <see cref="OverlayFadeOut"/> dependency property.</summary>
    public static readonly DependencyProperty OverlayFadeOutProperty
        = DependencyProperty.Register(nameof(OverlayFadeOut),
            typeof(Storyboard),
            typeof(MetroWindow),
            new PropertyMetadata(default(Storyboard)));

    /// <summary>
    /// Gets or sets the storyboard for the overlay fade out effect.
    /// </summary>
    public Storyboard OverlayFadeOut
    {
        get => (Storyboard) GetValue(OverlayFadeOutProperty);
        set => SetValue(OverlayFadeOutProperty, value);
    }

    /// <summary>Identifies the <see cref="WindowTransitionsEnabled"/> dependency property.</summary>
    public static readonly DependencyProperty WindowTransitionsEnabledProperty
        = DependencyProperty.Register(nameof(WindowTransitionsEnabled),
            typeof(bool),
            typeof(MetroWindow),
            new PropertyMetadata(BooleanBoxes.TrueBox));

    /// <summary>
    /// Gets or sets whether the start animation of the window content is available.
    /// </summary>
    public bool WindowTransitionsEnabled
    {
        get => (bool) GetValue(WindowTransitionsEnabledProperty);
        set => SetValue(WindowTransitionsEnabledProperty, BooleanBoxes.Box(value));
    }

    /// <summary>Identifies the <see cref="IconTemplate"/> dependency property.</summary>
    public static readonly DependencyProperty IconTemplateProperty
        = DependencyProperty.Register(nameof(IconTemplate),
            typeof(DataTemplate),
            typeof(MetroWindow),
            new PropertyMetadata(null, (o, e) =>
            {
                if (e.NewValue != e.OldValue)
                {
                    (o as MetroWindow)?.UpdateIconVisibility();
                }
            }));

    /// <summary>
    /// Gets or sets the <see cref="DataTemplate"/> for the icon on the TitleBar.
    /// </summary>
    public DataTemplate IconTemplate
    {
        get => (DataTemplate) GetValue(IconTemplateProperty);
        set => SetValue(IconTemplateProperty, value);
    }

    /// <summary>Identifies the <see cref="LeftWindowCommands"/> dependency property.</summary>
    public static readonly DependencyProperty LeftWindowCommandsProperty
        = DependencyProperty.Register(nameof(LeftWindowCommands),
            typeof(WindowCommands),
            typeof(MetroWindow),
            new PropertyMetadata(null, OnLeftWindowCommandsPropertyChanged));

    private static void OnLeftWindowCommandsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is WindowCommands windowCommands)
        {
            AutomationProperties.SetName(windowCommands, nameof(LeftWindowCommands));
        }

        UpdateLogicalChildren(d, e);
    }

    /// <summary>
    /// Gets or sets the <see cref="WindowCommands"/> host on the left side of the TitleBar.
    /// </summary>
    public WindowCommands LeftWindowCommands
    {
        get => (WindowCommands) GetValue(LeftWindowCommandsProperty);
        set => SetValue(LeftWindowCommandsProperty, value);
    }

    /// <summary>Identifies the <see cref="RightWindowCommands"/> dependency property.</summary>
    public static readonly DependencyProperty RightWindowCommandsProperty
        = DependencyProperty.Register(nameof(RightWindowCommands),
            typeof(WindowCommands),
            typeof(MetroWindow),
            new PropertyMetadata(null, OnRightWindowCommandsPropertyChanged));

    private static void OnRightWindowCommandsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is WindowCommands windowCommands)
        {
            AutomationProperties.SetName(windowCommands, nameof(RightWindowCommands));
        }

        UpdateLogicalChildren(d, e);
    }

    /// <summary>
    /// Gets or sets the <see cref="WindowCommands"/> host on the right side of the TitleBar.
    /// </summary>
    public WindowCommands RightWindowCommands
    {
        get => (WindowCommands) GetValue(RightWindowCommandsProperty);
        set => SetValue(RightWindowCommandsProperty, value);
    }

    /// <summary>Identifies the <see cref="WindowButtonCommands"/> dependency property.</summary>
    public static readonly DependencyProperty WindowButtonCommandsProperty
        = DependencyProperty.Register(nameof(WindowButtonCommands),
            typeof(WindowButtonCommands),
            typeof(MetroWindow),
            new PropertyMetadata(null, UpdateLogicalChildren));

    /// <summary>
    /// Gets or sets the <see cref="WindowButtonCommands"/> host that shows the minimize/maximize/restore/close buttons.
    /// </summary>
    public WindowButtonCommands WindowButtonCommands
    {
        get => (WindowButtonCommands) GetValue(WindowButtonCommandsProperty);
        set => SetValue(WindowButtonCommandsProperty, value);
    }

    /// <summary>Identifies the <see cref="LeftWindowCommandsOverlayBehavior"/> dependency property.</summary>
    public static readonly DependencyProperty LeftWindowCommandsOverlayBehaviorProperty
        = DependencyProperty.Register(nameof(LeftWindowCommandsOverlayBehavior),
            typeof(WindowCommandsOverlayBehavior),
            typeof(MetroWindow),
            new PropertyMetadata(WindowCommandsOverlayBehavior.Never, OnShowTitleBarPropertyChangedCallback));

    private static void OnShowTitleBarPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue != e.OldValue)
        {
            ((MetroWindow) d).UpdateTitleBarElementsVisibility();
        }
    }

    /// <summary>
    /// Gets or sets the overlay behavior for the <see cref="WindowCommands"/> host on the left side.
    /// </summary>
    public WindowCommandsOverlayBehavior LeftWindowCommandsOverlayBehavior
    {
        get => (WindowCommandsOverlayBehavior) GetValue(LeftWindowCommandsOverlayBehaviorProperty);
        set => SetValue(LeftWindowCommandsOverlayBehaviorProperty, value);
    }

    /// <summary>Identifies the <see cref="RightWindowCommandsOverlayBehavior"/> dependency property.</summary>
    public static readonly DependencyProperty RightWindowCommandsOverlayBehaviorProperty
        = DependencyProperty.Register(nameof(RightWindowCommandsOverlayBehavior),
            typeof(WindowCommandsOverlayBehavior),
            typeof(MetroWindow),
            new PropertyMetadata(WindowCommandsOverlayBehavior.Never, OnShowTitleBarPropertyChangedCallback));

    /// <summary>
    /// Gets or sets the overlay behavior for the <see cref="WindowCommands"/> host on the right side.
    /// </summary>
    public WindowCommandsOverlayBehavior RightWindowCommandsOverlayBehavior
    {
        get => (WindowCommandsOverlayBehavior) GetValue(RightWindowCommandsOverlayBehaviorProperty);
        set => SetValue(RightWindowCommandsOverlayBehaviorProperty, value);
    }

    /// <summary>Identifies the <see cref="WindowButtonCommandsOverlayBehavior"/> dependency property.</summary>
    public static readonly DependencyProperty WindowButtonCommandsOverlayBehaviorProperty
        = DependencyProperty.Register(nameof(WindowButtonCommandsOverlayBehavior),
            typeof(OverlayBehavior),
            typeof(MetroWindow),
            new PropertyMetadata(OverlayBehavior.Always, OnShowTitleBarPropertyChangedCallback));

    /// <summary>
    /// Gets or sets the overlay behavior for the <see cref="WindowButtonCommands"/> host.
    /// </summary>
    public OverlayBehavior WindowButtonCommandsOverlayBehavior
    {
        get => (OverlayBehavior) GetValue(WindowButtonCommandsOverlayBehaviorProperty);
        set => SetValue(WindowButtonCommandsOverlayBehaviorProperty, value);
    }

    /// <summary>Identifies the <see cref="IconOverlayBehavior"/> dependency property.</summary>
    public static readonly DependencyProperty IconOverlayBehaviorProperty
        = DependencyProperty.Register(nameof(IconOverlayBehavior),
            typeof(OverlayBehavior),
            typeof(MetroWindow),
            new PropertyMetadata(OverlayBehavior.Never, OnShowTitleBarPropertyChangedCallback));

    /// <summary>
    /// Gets or sets the overlay behavior for the <see cref="Window.Icon"/>.
    /// </summary>
    public OverlayBehavior IconOverlayBehavior
    {
        get => (OverlayBehavior) GetValue(IconOverlayBehaviorProperty);
        set => SetValue(IconOverlayBehaviorProperty, value);
    }

    /// <summary>Identifies the <see cref="OverrideDefaultWindowCommandsBrush"/> dependency property.</summary>
    public static readonly DependencyProperty OverrideDefaultWindowCommandsBrushProperty
        = DependencyProperty.Register(nameof(OverrideDefaultWindowCommandsBrush),
            typeof(Brush),
            typeof(MetroWindow));

    /// <summary>
    /// Allows easy handling of <see cref="WindowCommands"/> brush. Theme is also applied based on this brush.
    /// </summary>
    public Brush OverrideDefaultWindowCommandsBrush
    {
        get => (Brush) GetValue(OverrideDefaultWindowCommandsBrushProperty);
        set => SetValue(OverrideDefaultWindowCommandsBrushProperty, value);
    }

    /// <summary>Identifies the <see cref="IsWindowDraggable"/> dependency property.</summary>
    public static readonly DependencyProperty IsWindowDraggableProperty
        = DependencyProperty.Register(nameof(IsWindowDraggable),
            typeof(bool),
            typeof(MetroWindow),
            new PropertyMetadata(BooleanBoxes.TrueBox));

    /// <summary>
    /// Gets or sets whether the whole window is draggable.
    /// </summary>
    public bool IsWindowDraggable
    {
        get => (bool) GetValue(IsWindowDraggableProperty);
        set => SetValue(IsWindowDraggableProperty, BooleanBoxes.Box(value));
    }

    /// <summary>Identifies the <see cref="WindowTransitionCompleted"/> routed event.</summary>
    public static readonly RoutedEvent WindowTransitionCompletedEvent
        = EventManager.RegisterRoutedEvent(nameof(WindowTransitionCompleted),
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(MetroWindow));

    public event RoutedEventHandler WindowTransitionCompleted
    {
        add => AddHandler(WindowTransitionCompletedEvent, value);
        remove => RemoveHandler(WindowTransitionCompletedEvent, value);
    }

    private void UpdateIconVisibility()
    {
        var isVisible = (Icon is not null || IconTemplate is not null)
                        && ((IconOverlayBehavior.HasFlag(OverlayBehavior.HiddenTitleBar) && !ShowTitleBar) || (ShowIconOnTitleBar && ShowTitleBar));
        icon?.SetCurrentValue(VisibilityProperty, isVisible ? Visibility.Visible : Visibility.Collapsed);
    }

    private void UpdateTitleBarElementsVisibility()
    {
        UpdateIconVisibility();

        var newVisibility = TitleBarHeight > 0 && ShowTitleBar ? Visibility.Visible : Visibility.Collapsed;

        titleBar?.SetCurrentValue(VisibilityProperty, newVisibility);
        titleBarBackground?.SetCurrentValue(VisibilityProperty, newVisibility);

        var leftWindowCommandsVisibility = LeftWindowCommandsOverlayBehavior.HasFlag(WindowCommandsOverlayBehavior.HiddenTitleBar) ? Visibility.Visible : newVisibility;
        LeftWindowCommandsPresenter?.SetCurrentValue(VisibilityProperty, leftWindowCommandsVisibility);

        var rightWindowCommandsVisibility = RightWindowCommandsOverlayBehavior.HasFlag(WindowCommandsOverlayBehavior.HiddenTitleBar) ? Visibility.Visible : newVisibility;
        RightWindowCommandsPresenter?.SetCurrentValue(VisibilityProperty, rightWindowCommandsVisibility);

        var windowButtonCommandsVisibility = WindowButtonCommandsOverlayBehavior.HasFlag(OverlayBehavior.HiddenTitleBar) ? Visibility.Visible : newVisibility;
        WindowButtonCommandsPresenter?.SetCurrentValue(VisibilityProperty, windowButtonCommandsVisibility);

        SetWindowEvents();
    }

    static MetroWindow()
    {
        var windowCommandFields = typeof(WindowCommands)
            .GetFields(BindingFlags.Static | BindingFlags.NonPublic)
            .ToArray();
        var parentWindowField = windowCommandFields.FirstOrDefault(x => x.Name == nameof(ParentWindowPropertyKey));
        if (parentWindowField == null)
        {
            throw new InvalidStateException($"Failed to find field {nameof(ParentWindowPropertyKey)} in {typeof(WindowCommands)}, candidates: {windowCommandFields.Select(x => new {x.Name, x.FieldType}).DumpToString()}");
        }

        ParentWindowPropertyKey = (DependencyPropertyKey)parentWindowField.GetValue(null);
        if (ParentWindowPropertyKey == null)
        {
            throw new InvalidStateException($"Failed to get value of field {parentWindowField} in {typeof(WindowCommands)}");
        }
        DefaultStyleKeyProperty.OverrideMetadata(typeof(MetroWindow), new FrameworkPropertyMetadata(typeof(MetroWindow)));

        IconProperty.OverrideMetadata(
            typeof(MetroWindow),
            new FrameworkPropertyMetadata(
                (o, e) =>
                {
                    if (e.NewValue != e.OldValue)
                    {
                        (o as MetroWindow)?.UpdateIconVisibility();
                    }
                }));
    }

    /// <summary>
    /// Initializes a new instance of the MahApps.Metro.Controls.MetroWindow class.
    /// </summary>
    public MetroWindow()
    {
        DataContextChanged += MetroWindow_DataContextChanged;
    }

    private void MetroWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        // MahApps add these controls to the window with AddLogicalChild method.
        // This has the side effect that the DataContext doesn't update, so do this now here.
        if (LeftWindowCommands != null)
        {
            LeftWindowCommands.DataContext = DataContext;
        }

        if (RightWindowCommands != null)
        {
            RightWindowCommands.DataContext = DataContext;
        }

        if (WindowButtonCommands != null)
        {
            WindowButtonCommands.DataContext = DataContext;
        }
    }

    private void MetroWindow_SizeChanged(object sender, RoutedEventArgs e)
    {
        // this all works only for centered title
        if (TitleAlignment != HorizontalAlignment.Center
            || titleBar is null)
        {
            return;
        }

        // Half of this MetroWindow
        var halfDistance = ActualWidth / 2;
        // Distance between center and left/right
        var margin = (Thickness) titleBar.GetValue(MarginProperty);
        var distanceToCenter = (titleBar.DesiredSize.Width - margin.Left - margin.Right) / 2;

        var iconWidth = icon?.ActualWidth ?? 0;
        var leftWindowCommandsWidth = LeftWindowCommands?.ActualWidth ?? 0;
        var rightWindowCommandsWidth = RightWindowCommands?.ActualWidth ?? 0;
        var windowButtonCommandsWith = WindowButtonCommands?.ActualWidth ?? 0;

        // Distance between right edge from LeftWindowCommands to left window side
        var distanceFromLeft = iconWidth + leftWindowCommandsWidth;
        // Distance between left edge from RightWindowCommands to right window side
        var distanceFromRight = rightWindowCommandsWidth + windowButtonCommandsWith;
        // Margin
        const double horizontalMargin = 5.0;

        var dLeft = distanceFromLeft + distanceToCenter + horizontalMargin;
        var dRight = distanceFromRight + distanceToCenter + horizontalMargin;
        if ((dLeft < halfDistance) && (dRight < halfDistance))
        {
            titleBar.SetCurrentValue(MarginProperty, default(Thickness));
            Grid.SetColumn(titleBar, 0);
            Grid.SetColumnSpan(titleBar, 3);
        }
        else
        {
            titleBar.SetCurrentValue(MarginProperty, new Thickness(leftWindowCommandsWidth, 0, rightWindowCommandsWidth, 0));
            Grid.SetColumn(titleBar, 1);
            Grid.SetColumnSpan(titleBar, 1);
        }
    }

    private static void UpdateLogicalChildren(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not MetroWindow window)
        {
            return;
        }

        if (e.OldValue is FrameworkElement oldChild)
        {
            window.RemoveLogicalChild(oldChild);
        }

        if (e.NewValue is FrameworkElement newChild)
        {
            window.AddLogicalChild(newChild);
            newChild.DataContext = window.DataContext;
        }
    }

    /// <inheritdoc />
    protected override IEnumerator LogicalChildren
    {
        get
        {
            // cheat, make a list with all logical content and return the enumerator
            var children = new ArrayList();
            if (Content != null)
            {
                children.Add(Content);
            }

            if (LeftWindowCommands != null)
            {
                children.Add(LeftWindowCommands);
            }

            if (RightWindowCommands != null)
            {
                children.Add(RightWindowCommands);
            }

            if (WindowButtonCommands != null)
            {
                children.Add(WindowButtonCommands);
            }

            return children.GetEnumerator();
        }
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        LeftWindowCommandsPresenter = GetTemplateChild(PART_LeftWindowCommands) as ContentPresenter;
        RightWindowCommandsPresenter = GetTemplateChild(PART_RightWindowCommands) as ContentPresenter;
        WindowButtonCommandsPresenter = GetTemplateChild(PART_WindowButtonCommands) as ContentPresenter;

        LeftWindowCommands ??= new WindowCommands();
        RightWindowCommands ??= new WindowCommands();
        WindowButtonCommands ??= new WindowButtonCommands();

        LeftWindowCommands.SetValue(ParentWindowPropertyKey, this);
        RightWindowCommands.SetValue(ParentWindowPropertyKey, this);
        WindowButtonCommands.SetValue(ParentWindowPropertyKey, this);

        icon = GetTemplateChild(PART_Icon) as FrameworkElement;
        titleBar = GetTemplateChild(PART_TitleBar) as UIElement;
        titleBarBackground = GetTemplateChild(PART_WindowTitleBackground) as UIElement;
        windowTitleThumb = GetTemplateChild(PART_WindowTitleThumb) as Thumb;

        UpdateTitleBarElementsVisibility();

        if (GetTemplateChild(PART_Content) is MetroContentControl metroContentControl)
        {
            metroContentControl.TransitionCompleted += (_, _) => RaiseEvent(new RoutedEventArgs(WindowTransitionCompletedEvent));
        }
    }

    /// <summary>
    /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
    /// </summary>
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new MetroWindowAutomationPeer(this);
    }

    protected internal IntPtr CriticalHandle
    {
        get
        {
            VerifyAccess();
            var value = typeof(Window).GetProperty("CriticalHandle", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(this, Array.Empty<object>()) ?? IntPtr.Zero;
            return (IntPtr) value;
        }
    }

    private void ClearWindowEvents()
    {
        if (windowTitleThumb != null)
        {
            windowTitleThumb.PreviewMouseLeftButtonUp -= WindowTitleThumbOnPreviewMouseLeftButtonUp;
            windowTitleThumb.DragDelta -= WindowTitleThumbMoveOnDragDelta;
            windowTitleThumb.MouseDoubleClick -= WindowTitleThumbChangeWindowStateOnMouseDoubleClick;
            windowTitleThumb.MouseRightButtonUp -= WindowTitleThumbSystemMenuOnMouseRightButtonUp;
        }

        if (titleBar is IMetroThumb thumbContentControl)
        {
            thumbContentControl.PreviewMouseLeftButtonUp -= WindowTitleThumbOnPreviewMouseLeftButtonUp;
            thumbContentControl.DragDelta -= WindowTitleThumbMoveOnDragDelta;
            thumbContentControl.MouseDoubleClick -= WindowTitleThumbChangeWindowStateOnMouseDoubleClick;
            thumbContentControl.MouseRightButtonUp -= WindowTitleThumbSystemMenuOnMouseRightButtonUp;
        }

        if (icon != null)
        {
            icon.MouseLeftButtonDown -= OnIconMouseLeftButtonDown;
        }

        SizeChanged -= MetroWindow_SizeChanged;
    }

    private void SetWindowEvents()
    {
        // clear all event handlers first
        ClearWindowEvents();

        // set mouse down/up for icon
        if (icon is {Visibility: Visibility.Visible})
        {
            icon.MouseDown += OnIconMouseLeftButtonDown;
        }

        if (windowTitleThumb != null)
        {
            windowTitleThumb.PreviewMouseLeftButtonUp += WindowTitleThumbOnPreviewMouseLeftButtonUp;
            windowTitleThumb.DragDelta += WindowTitleThumbMoveOnDragDelta;
            windowTitleThumb.MouseDoubleClick += WindowTitleThumbChangeWindowStateOnMouseDoubleClick;
            windowTitleThumb.MouseRightButtonUp += WindowTitleThumbSystemMenuOnMouseRightButtonUp;
        }

        if (titleBar is IMetroThumb thumbContentControl)
        {
            thumbContentControl.PreviewMouseLeftButtonUp += WindowTitleThumbOnPreviewMouseLeftButtonUp;
            thumbContentControl.DragDelta += WindowTitleThumbMoveOnDragDelta;
            thumbContentControl.MouseDoubleClick += WindowTitleThumbChangeWindowStateOnMouseDoubleClick;
            thumbContentControl.MouseRightButtonUp += WindowTitleThumbSystemMenuOnMouseRightButtonUp;
        }

        // handle size if we have a Grid for the title (e.g. clean window have a centered title)
        if (titleBar != null && TitleAlignment == HorizontalAlignment.Center)
        {
            SizeChanged += MetroWindow_SizeChanged;
        }
    }

    private void OnIconMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && CloseOnIconDoubleClick)
        {
            Close();
        }
        else if (ShowSystemMenu)
        {
#pragma warning disable 618
            ControlzEx.SystemCommands.ShowSystemMenuPhysicalCoordinates(this, PointToScreen(new Point(BorderThickness.Left, TitleBarHeight + BorderThickness.Top)));
#pragma warning restore 618
        }
    }

    private void WindowTitleThumbOnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        DoWindowTitleThumbOnPreviewMouseLeftButtonUp(this, e);
    }

    private void WindowTitleThumbMoveOnDragDelta(object sender, DragDeltaEventArgs dragDeltaEventArgs)
    {
        DoWindowTitleThumbMoveOnDragDelta(sender as IMetroThumb, this, dragDeltaEventArgs);
    }

    private void WindowTitleThumbChangeWindowStateOnMouseDoubleClick(object sender, MouseButtonEventArgs mouseButtonEventArgs)
    {
        DoWindowTitleThumbChangeWindowStateOnMouseDoubleClick(this, mouseButtonEventArgs);
    }

    private void WindowTitleThumbSystemMenuOnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        DoWindowTitleThumbSystemMenuOnMouseRightButtonUp(this, e);
    }

    internal static void DoWindowTitleThumbOnPreviewMouseLeftButtonUp(MetroWindow window, MouseButtonEventArgs mouseButtonEventArgs)
    {
        if (mouseButtonEventArgs.Source == mouseButtonEventArgs.OriginalSource)
        {
            Mouse.Capture(null);
        }
    }

    internal static void DoWindowTitleThumbMoveOnDragDelta(IMetroThumb thumb, MetroWindow window, DragDeltaEventArgs dragDeltaEventArgs)
    {
        if (thumb is null)
        {
            throw new ArgumentNullException(nameof(thumb));
        }

        if (window is null)
        {
            throw new ArgumentNullException(nameof(window));
        }

        // drag only if IsWindowDraggable is set to true
        if (!window.IsWindowDraggable ||
            (!(Math.Abs(dragDeltaEventArgs.HorizontalChange) > 2) && !(Math.Abs(dragDeltaEventArgs.VerticalChange) > 2)))
        {
            return;
        }

        // This was taken from DragMove internal code
        window.VerifyAccess();

        // if the window is maximized dragging is only allowed on title bar (also if not visible)
        var windowIsMaximized = window.WindowState == WindowState.Maximized;
        var isMouseOnTitlebar = Mouse.GetPosition(thumb).Y <= window.TitleBarHeight && window.TitleBarHeight > 0;
        if (!isMouseOnTitlebar && windowIsMaximized)
        {
            return;
        }

        // for the touch usage
        User32.ReleaseCapture();

        if (windowIsMaximized)
        {
            EventHandler onWindowStateChanged = null;
            onWindowStateChanged = (sender, args) =>
            {
                window.StateChanged -= onWindowStateChanged;

                if (window.WindowState == WindowState.Normal)
                {
                    Mouse.Capture(thumb, CaptureMode.Element);
                }
            };

            window.StateChanged -= onWindowStateChanged;
            window.StateChanged += onWindowStateChanged;
        }

        var wpfPoint = window.PointToScreen(Mouse.GetPosition(window));
        var x = (int) wpfPoint.X;
        var y = (int) wpfPoint.Y;

        User32.SendMessage(
            window.CriticalHandle, 
            User32.WindowMessage.WM_NCLBUTTONDOWN, 
            (IntPtr)HT.CAPTION,
            new IntPtr(x | (y << 16)));
    }

    internal static void DoWindowTitleThumbChangeWindowStateOnMouseDoubleClick(MetroWindow window, MouseButtonEventArgs mouseButtonEventArgs)
    {
        // restore/maximize only with left button
        if (mouseButtonEventArgs.ChangedButton == MouseButton.Left)
        {
            // we can maximize or restore the window if the title bar height is set (also if title bar is hidden)
            var canResize = window.ResizeMode == ResizeMode.CanResizeWithGrip || window.ResizeMode == ResizeMode.CanResize;
            var mousePos = Mouse.GetPosition(window);
            var isMouseOnTitlebar = mousePos.Y <= window.TitleBarHeight && window.TitleBarHeight > 0;
            if (canResize && isMouseOnTitlebar)
            {
#pragma warning disable 618
                if (window.WindowState == WindowState.Normal)
                {
                    ControlzEx.SystemCommands.MaximizeWindow(window);
                }
                else
                {
                    ControlzEx.SystemCommands.RestoreWindow(window);
                }
#pragma warning restore 618
                mouseButtonEventArgs.Handled = true;
            }
        }
    }

    private static void DoWindowTitleThumbSystemMenuOnMouseRightButtonUp(MetroWindow window, MouseButtonEventArgs e)
    {
        if (window.ShowSystemMenuOnRightClick)
        {
            // show menu only if mouse pos is on title bar or if we have a window with none style and no title bar
            var mousePos = e.GetPosition(window);
            if ((mousePos.Y <= window.TitleBarHeight && window.TitleBarHeight > 0) || (window.WindowStyle == WindowStyle.None && window.TitleBarHeight <= 0))
            {
#pragma warning disable 618
                ControlzEx.SystemCommands.ShowSystemMenuPhysicalCoordinates(window, window.PointToScreen(mousePos));
#pragma warning restore 618
            }
        }
    }

    /// <summary>
    /// Gets the template child with the given name.
    /// </summary>
    /// <typeparam name="T">The interface type inherited from DependencyObject.</typeparam>
    /// <param name="name">The name of the template child.</param>
    internal T GetPart<T>(string name)
        where T : class
    {
        return GetTemplateChild(name) as T;
    }
}