using System;
using System.Drawing;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Media;
using PoeShared.Blazor.Wpf.Services;
using PoeShared.Scaffolding;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace PoeShared.Blazor.Wpf.Scaffolding;

internal sealed class FrameworkElementLocationTracker : DisposableReactiveObject, IBlazorControlLocationTracker
{
    public FrameworkElementLocationTracker(FrameworkElement frameworkElement)
    {
        FrameworkElement = frameworkElement;
        if (FrameworkElement.IsLoaded)
        {
            HandleLoaded();
        }
        else
        {
            FrameworkElement.Loaded += FrameworkElementOnLoaded;
            Disposable.Create(() =>
            {
                FrameworkElement.Loaded -= FrameworkElementOnLoaded;
            }).AddTo(Anchors);
        }
    }

    private void FrameworkElementOnLoaded(object sender, RoutedEventArgs e)
    {
        FrameworkElement.Loaded -= FrameworkElementOnLoaded;
        HandleLoaded();
    }

    public FrameworkElement FrameworkElement { get; }

    public Window Parent { get; private set; }
    
    public Rectangle BoundsOnScreen { get; private set; } 

    private void HandleLoaded()
    {
        Parent = Window.GetWindow(FrameworkElement);
        if (Parent == null)
        {
            return;
        }
        Parent.LocationChanged += ParentOnLocationChanged;
        Parent.SizeChanged += ParentOnLocationChanged;
        Parent.DpiChanged += ParentOnDpiChanged; 
        FrameworkElement.Unloaded += FrameworkElementOnUnloaded; 
        FrameworkElement.SizeChanged += FrameworkElementOnSizeChanged;
        
        Disposable.Create(() =>
        {
            Parent.LocationChanged -= ParentOnLocationChanged;
            Parent.SizeChanged -= ParentOnLocationChanged;
            Parent.DpiChanged -= ParentOnDpiChanged;
            FrameworkElement.Unloaded -= FrameworkElementOnUnloaded; 
        }).AddTo(Anchors);

        UpdateBounds();
    }

    private void FrameworkElementOnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateBounds();
    }

    private void FrameworkElementOnUnloaded(object sender, RoutedEventArgs e)
    {
        Dispose();
    }
    
    private void ParentOnDpiChanged(object sender, DpiChangedEventArgs e)
    {
        UpdateBounds();
    }

    private void ParentOnLocationChanged(object sender, EventArgs e)
    {
        UpdateBounds();
    }

    private void UpdateBounds()
    {
        var rect = GetControlScreenCoordinates(FrameworkElement, Parent);
        var dpi = VisualTreeHelper.GetDpi(FrameworkElement);
        var screenRect = rect.ScaleToScreen(new PointF((float)dpi.DpiScaleX, (float)dpi.DpiScaleY));
        BoundsOnScreen = rect.ToWinRectangle();
    }
    
    private static Rect GetControlScreenCoordinates(FrameworkElement control, Window window)
    {
        var relativePoint = control.TransformToAncestor(window).Transform(new Point(0, 0));
        var screenPoint = window.PointToScreen(relativePoint);
        var size = new Size(control.ActualWidth, control.ActualHeight);
        return new Rect(screenPoint, size);
    }
}