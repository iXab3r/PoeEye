﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace PoeShared.UI;

/// <summary>
/// A content control that allows an adorner for the content to
/// be defined in XAML.
/// Source: http://www.codeproject.com/Articles/54472/Defining-WPF-Adorners-in-XAML
/// </summary>
public class AdornedControl : ContentControl
{
    /// <summary>
    /// Dependency properties.
    /// </summary>
    public static readonly DependencyProperty IsAdornerVisibleProperty =
        DependencyProperty.Register("IsAdornerVisible", typeof(bool), typeof(AdornedControl),
            new FrameworkPropertyMetadata(defaultValue: false, IsAdornerVisible_PropertyChanged));

    public static readonly DependencyProperty AdornerContentProperty =
        DependencyProperty.Register("AdornerContent", typeof(FrameworkElement), typeof(AdornedControl),
            new FrameworkPropertyMetadata(AdornerContent_PropertyChanged));

    public static readonly DependencyProperty HorizontalAdornerPlacementProperty =
        DependencyProperty.Register("HorizontalAdornerPlacement", typeof(AdornerPlacement), typeof(AdornedControl),
            new FrameworkPropertyMetadata(AdornerPlacement.Inside));

    public static readonly DependencyProperty VerticalAdornerPlacementProperty =
        DependencyProperty.Register("VerticalAdornerPlacement", typeof(AdornerPlacement), typeof(AdornedControl),
            new FrameworkPropertyMetadata(AdornerPlacement.Inside));

    public static readonly DependencyProperty AdornerOffsetXProperty =
        DependencyProperty.Register("AdornerOffsetX", typeof(double), typeof(AdornedControl));
    public static readonly DependencyProperty AdornerOffsetYProperty =
        DependencyProperty.Register("AdornerOffsetY", typeof(double), typeof(AdornedControl));

    /// <summary>
    /// Commands.
    /// </summary>
    public static readonly RoutedCommand ShowAdornerCommand = new RoutedCommand("ShowAdorner", typeof(AdornedControl));
    public static readonly RoutedCommand HideAdornerCommand = new RoutedCommand("HideAdorner", typeof(AdornedControl));

    public AdornedControl()
    {
        Focusable = false; // By default don't want 'AdornedControl' to be focusable.

        DataContextChanged += new DependencyPropertyChangedEventHandler(AdornedControl_DataContextChanged);
    }

    /// <summary>
    /// Event raised when the DataContext of the adorned control changes.
    /// </summary>
    private void AdornedControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        UpdateAdornerDataContext();
    }

    /// <summary>
    /// Update the DataContext of the adorner from the adorned control.
    /// </summary>
    private void UpdateAdornerDataContext()
    {
        if (AdornerContent != null)
        {
            AdornerContent.DataContext = DataContext;
        }
    }

    /// <summary>
    /// Show the adorner.
    /// </summary>
    public void ShowAdorner()
    {
        IsAdornerVisible = true;
    }

    /// <summary>
    /// Hide the adorner.
    /// </summary>
    public void HideAdorner()
    {
        IsAdornerVisible = false;
    }

    /// <summary>
    /// Shows or hides the adorner.
    /// Set to 'true' to show the adorner or 'false' to hide the adorner.
    /// </summary>
    public bool IsAdornerVisible
    {
        get => (bool)GetValue(IsAdornerVisibleProperty);
        set => SetValue(IsAdornerVisibleProperty, value);
    }

    /// <summary>
    /// Used in XAML to define the UI content of the adorner.
    /// </summary>
    public FrameworkElement AdornerContent
    {
        get => (FrameworkElement)GetValue(AdornerContentProperty);
        set => SetValue(AdornerContentProperty, value);
    }

    /// <summary>
    /// Specifies the horizontal placement of the adorner relative to the adorned control.
    /// </summary>
    public AdornerPlacement HorizontalAdornerPlacement
    {
        get => (AdornerPlacement)GetValue(HorizontalAdornerPlacementProperty);
        set => SetValue(HorizontalAdornerPlacementProperty, value);
    }

    /// <summary>
    /// Specifies the vertical placement of the adorner relative to the adorned control.
    /// </summary>
    public AdornerPlacement VerticalAdornerPlacement
    {
        get => (AdornerPlacement)GetValue(VerticalAdornerPlacementProperty);
        set => SetValue(VerticalAdornerPlacementProperty, value);
    }

    /// <summary>
    /// X offset of the adorner.
    /// </summary>
    public double AdornerOffsetX
    {
        get => (double)GetValue(AdornerOffsetXProperty);
        set => SetValue(AdornerOffsetXProperty, value);
    }

    /// <summary>
    /// Y offset of the adorner.
    /// </summary>
    public double AdornerOffsetY
    {
        get => (double)GetValue(AdornerOffsetYProperty);
        set => SetValue(AdornerOffsetYProperty, value);
    }

    /// <summary>
    /// Command bindings.
    /// </summary>
    private static readonly CommandBinding ShowAdornerCommandBinding = new CommandBinding(ShowAdornerCommand, ShowAdornerCommand_Executed);
    private static readonly CommandBinding HideAdornerCommandBinding = new CommandBinding(HideAdornerCommand, HideAdornerCommand_Executed);

    /// <summary>
    /// Caches the adorner layer.
    /// </summary>
    private AdornerLayer adornerLayer = null;

    /// <summary>
    /// The actual adorner create to contain our 'adorner UI content'.
    /// </summary>
    private FrameworkElementAdorner adorner = null;

    /// <summary>
    /// Static constructor to register command bindings.
    /// </summary>
    static AdornedControl()
    {
        CommandManager.RegisterClassCommandBinding(typeof(AdornedControl), ShowAdornerCommandBinding);
        CommandManager.RegisterClassCommandBinding(typeof(AdornedControl), HideAdornerCommandBinding);
    }

    /// <summary>
    /// Event raised when the Show command is executed.
    /// </summary>
    private static void ShowAdornerCommand_Executed(object target, ExecutedRoutedEventArgs e)
    {
        var c = (AdornedControl)target;
        c.ShowAdorner();
    }

    /// <summary>
    /// Event raised when the Hide command is executed.
    /// </summary>
    private static void HideAdornerCommand_Executed(object target, ExecutedRoutedEventArgs e)
    {
        var c = (AdornedControl)target;
        c.HideAdorner();
    }

    /// <summary>
    /// Event raised when the value of IsAdornerVisible has changed.
    /// </summary>
    private static void IsAdornerVisible_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
    {
        var c = (AdornedControl)o;
        c.ShowOrHideAdornerInternal();
    }

    /// <summary>
    /// Event raised when the value of AdornerContent has changed.
    /// </summary>
    private static void AdornerContent_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
    {
        var c = (AdornedControl)o;
        c.ShowOrHideAdornerInternal();
    }

    /// <summary>
    /// Internal method to show or hide the adorner based on the value of IsAdornerVisible.
    /// </summary>
    private void ShowOrHideAdornerInternal()
    {
        if (IsAdornerVisible)
        {
            ShowAdornerInternal();
        }
        else
        {
            HideAdornerInternal();
        }
    }

    /// <summary>
    /// Internal method to show the adorner.
    /// </summary>
    private void ShowAdornerInternal()
    {
        if (adorner != null)
        {
            // Already adorned.
            return;
        }

        if (AdornerContent == null)
        {
            return;
        }

        if (adornerLayer == null)
        {
            adornerLayer = AdornerLayer.GetAdornerLayer(this);
        }

        if (adornerLayer == null)
        {
            return;
        }

        adorner = new FrameworkElementAdorner(
            AdornerContent, 
            this, HorizontalAdornerPlacement, VerticalAdornerPlacement,
            AdornerOffsetX, AdornerOffsetY);
        adornerLayer.Add(adorner);
        UpdateAdornerDataContext();
    }

    /// <summary>
    /// Internal method to hide the adorner.
    /// </summary>
    private void HideAdornerInternal()
    {
        if (adornerLayer == null || adorner == null)
        {
            // Not already adorned.
            return;
        }

        adornerLayer.Remove(adorner);
        adorner.DisconnectChild();

        adorner = null;
        adornerLayer = null;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        ShowOrHideAdornerInternal();
    }
}