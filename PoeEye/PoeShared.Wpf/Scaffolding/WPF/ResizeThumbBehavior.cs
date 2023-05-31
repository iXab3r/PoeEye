using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interactivity;
using MaterialDesignThemes.Wpf;

namespace PoeShared.Scaffolding.WPF;

public sealed class ResizeThumbBehavior : Behavior<Thumb>
{
    public static readonly DependencyProperty ElementToResizeProperty = DependencyProperty.RegisterAttached(nameof(ElementToResize), typeof(FrameworkElement), typeof(ResizeThumbBehavior), new UIPropertyMetadata(null));

    public FrameworkElement ElementToResize
    {
        get => (FrameworkElement) GetValue(ElementToResizeProperty);
        set => SetValue(ElementToResizeProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.DragDelta += OnAssociatedObjectOnDragDelta;

        if (ElementToResize != null)
        {
            return;
        }
        
        AssociatedObject.Loaded += AssociatedObjectOnLoaded;
    }

    private void AssociatedObjectOnLoaded(object sender, RoutedEventArgs e)
    {
        ElementToResize = 
            AssociatedObject.FindLogicalAncestor<Popup>() ??
            (FrameworkElement)AssociatedObject.FindLogicalAncestor<Window>();

        if (ElementToResize == null)
        {
            throw new ArgumentException("Failed to find element to resize");
        }
    }

    private void OnAssociatedObjectOnDragDelta(object sender, DragDeltaEventArgs args)
    {
        var t = (Thumb) sender;
        if (t.Cursor == Cursors.SizeWE || t.Cursor == Cursors.SizeNWSE || t.Cursor == Cursors.SizeNESW)
        {
            var newWidth = ElementToResize.Width + args.HorizontalChange;
            ElementToResize.Width = newWidth.EnsureInRange(ElementToResize.MinWidth, ElementToResize.MaxWidth);
        }

        if (t.Cursor == Cursors.SizeNS || t.Cursor == Cursors.SizeNWSE || t.Cursor == Cursors.SizeNESW)
        {
            var newHeight = ElementToResize.Height + args.VerticalChange;
            ElementToResize.Height = newHeight.EnsureInRange(ElementToResize.MinHeight, ElementToResize.MaxHeight);
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.DragDelta -= OnAssociatedObjectOnDragDelta;
    }
}