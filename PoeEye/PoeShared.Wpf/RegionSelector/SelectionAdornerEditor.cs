using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using App.Metrics;
using JetBrains.Annotations;
using PoeShared.Native;
using PoeShared.Scaffolding;
using PoeShared.UI;
using PropertyBinder;
using ReactiveUI;

namespace PoeShared.RegionSelector;

public class SelectionAdornerEditor : ReactiveControl
{
    private static readonly Binder<SelectionAdornerEditor> Binder = new();

    public static readonly DependencyProperty IsInEditModeProperty = DependencyProperty.Register(
        nameof(IsInEditMode), typeof(bool), typeof(SelectionAdornerEditor), new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty ShowProjectedProperty = DependencyProperty.Register(
        nameof(ShowProjected), typeof(bool), typeof(SelectionAdornerEditor), new PropertyMetadata(default(bool)));

    public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
        nameof(Stroke), typeof(Brush), typeof(SelectionAdornerEditor), new PropertyMetadata(Brushes.Lime));

    public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
        nameof(StrokeThickness), typeof(double), typeof(SelectionAdornerEditor), new PropertyMetadata((double) 2));

    public static readonly DependencyProperty ShowCrosshairProperty = DependencyProperty.Register(
        nameof(ShowCrosshair), typeof(bool), typeof(SelectionAdornerEditor), new PropertyMetadata(true));

    public static readonly DependencyProperty ShowBackgroundProperty = DependencyProperty.Register(
        nameof(ShowBackground), typeof(bool), typeof(SelectionAdornerEditor), new PropertyMetadata(true));

    public static readonly DependencyProperty BackgroundOpacityProperty = DependencyProperty.Register(
        nameof(BackgroundOpacity), typeof(double), typeof(SelectionAdornerEditor), new PropertyMetadata(0.5));

    public static readonly DependencyProperty ProjectionBoundsProperty = DependencyProperty.Register(
        nameof(ProjectionBounds), typeof(WinRect), typeof(SelectionAdornerEditor), new FrameworkPropertyMetadata(default(WinRect), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty SelectionProjectedProperty = DependencyProperty.Register(
        nameof(SelectionProjected), typeof(WinRect), typeof(SelectionAdornerEditor), new FrameworkPropertyMetadata(default(WinRect), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty MousePositionProjectedProperty = DependencyProperty.Register(
        nameof(MousePositionProjected), typeof(WinPoint), typeof(SelectionAdornerEditor), new FrameworkPropertyMetadata(default(WinPoint), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty IsBoxSelectionEnabledProperty = DependencyProperty.Register(
        nameof(IsBoxSelectionEnabled), typeof(bool), typeof(SelectionAdornerEditor), new FrameworkPropertyMetadata(true));

    static SelectionAdornerEditor()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(SelectionAdornerEditor), new FrameworkPropertyMetadata(typeof(SelectionAdornerEditor)));

        Binder.Bind(x => new WpfSize(x.ActualWidth, x.ActualHeight)).To(x => x.ActualSize);

        Binder
            .Bind(x => ScreenRegionUtils.CalculateProjection(
                new WpfRect(x.MousePosition, new WpfSize(1, 1)),
                x.ActualSize,
                x.ProjectionBounds).Location)
            .To((x, v) => x.SetCurrentValue(MousePositionProjectedProperty, v));

        Binder.Bind(x => ScreenRegionUtils.CalculateProjection(
                x.SelectionProjected,
                x.ActualSize,
                x.ProjectionBounds
            ))
            .To((x, v) => x.Selection = v);
    }

    public SelectionAdornerEditor()
    {
        Binder.Attach(this).AddTo(Anchors);
    }

    public bool IsInEditMode
    {
        get => (bool) GetValue(IsInEditModeProperty);
        set => SetValue(IsInEditModeProperty, value);
    }

    public bool ShowProjected
    {
        get => (bool) GetValue(ShowProjectedProperty);
        set => SetValue(ShowProjectedProperty, value);
    }

    public Brush Stroke
    {
        get => (Brush) GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public double StrokeThickness
    {
        get => (double) GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public bool ShowCrosshair
    {
        get => (bool) GetValue(ShowCrosshairProperty);
        set => SetValue(ShowCrosshairProperty, value);
    }

    public bool ShowBackground
    {
        get => (bool) GetValue(ShowBackgroundProperty);
        set => SetValue(ShowBackgroundProperty, value);
    }

    public double BackgroundOpacity
    {
        get => (double) GetValue(BackgroundOpacityProperty);
        set => SetValue(BackgroundOpacityProperty, value);
    }

    public WinRect ProjectionBounds
    {
        get => (WinRect) GetValue(ProjectionBoundsProperty);
        set => SetValue(ProjectionBoundsProperty, value);
    }

    public WinRect SelectionProjected
    {
        get => (WinRect) GetValue(SelectionProjectedProperty);
        set => SetValue(SelectionProjectedProperty, value);
    }

    public WinPoint MousePositionProjected
    {
        get => (WinPoint) GetValue(MousePositionProjectedProperty);
        set => SetValue(MousePositionProjectedProperty, value);
    }

    public bool IsBoxSelectionEnabled
    {
        get => (bool) GetValue(IsBoxSelectionEnabledProperty);
        set => SetValue(IsBoxSelectionEnabledProperty, value);
    }

    public WpfRect Selection { get; private set; }

    public WpfPoint MousePosition { get; private set; }

    public WpfPoint AnchorPoint { get; private set; }

    private WpfSize ActualSize { get; [UsedImplicitly] set; }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        var mouseEvents = Observable.Using(() =>
        {
            MouseMove += OnMouseMove;
            PreviewMouseDown += OnPreviewMouseDown;
            PreviewMouseUp += OnPreviewMouseUp;
            CaptureMouse();
            Focus();

            return Disposable.Create(() =>
            {
                MouseMove -= OnMouseMove;
                PreviewMouseDown -= OnPreviewMouseDown;
                PreviewMouseUp -= OnPreviewMouseUp;
                ReleaseMouseCapture();
            });
        }, disposable => Observable.Never<Unit>());

        var isInEditModeSource = this.WhenAnyValue(x => x.IsInEditMode);

        var keyboardFocusLost = this.Observe(IsKeyboardFocusWithinProperty, x => x.IsKeyboardFocusWithin)
            .Where(x => x == false)
            .Select(x => true);

        var keyDownSource = Observable.FromEventPattern<KeyEventHandler, KeyEventArgs>(h => PreviewKeyDown += h, h => PreviewKeyDown -= h)
            .Where(x => x.EventArgs.Key == Key.Escape)
            .Select(x => true);

        var mouseDownSource = Observable.FromEventPattern<MouseButtonEventHandler, MouseButtonEventArgs>(h => PreviewMouseDown += h, h => PreviewMouseDown -= h)
            .Where(x => x.EventArgs.ChangedButton == MouseButton.Right)
            .Select(x => true);

        var cancelEditModeSource = Observable.Merge(
            keyboardFocusLost,
            keyDownSource,
            mouseDownSource);

        isInEditModeSource
            .Select(x => x ? mouseEvents : Observable.Empty<Unit>())
            .Switch()
            .Subscribe()
            .AddTo(Anchors);

        isInEditModeSource
            .Select(x => x ? cancelEditModeSource : Observable.Empty<bool>())
            .Switch()
            .Subscribe(_ =>
            {
                AnchorPoint = default;
                IsInEditMode = false;
            })
            .AddTo(Anchors);
    }

    private void OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (AnchorPoint.IsEmpty())
        {
            return;
        }

        try
        {
            var mousePosition = e.GetPosition(this);
            if (IsBoxSelectionEnabled && e.ChangedButton == MouseButton.Left && TryToCalculateSelection(mousePosition, AnchorPoint, ActualSize, out var selection))
            {
                AdaptSelection(selection);
                e.Handled = true;
            }
        }
        finally
        {
            AnchorPoint = default;
        }
    }

    private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
        {
            return;
        }

        var mousePosition = e.GetPosition(this);
        AnchorPoint = mousePosition;

        e.Handled = true;
        if (!IsBoxSelectionEnabled && TryToCalculateSelectionForPoint(mousePosition,  AnchorPoint, ActualSize, out var selection))
        {
            AdaptSelection(selection);
        }
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        MousePosition = CalculatePosition(e.GetPosition(this), ActualSize);
        if (AnchorPoint.IsEmpty())
        {
            return;
        }

        if (IsBoxSelectionEnabled)
        {
            if (TryToCalculateSelection(MousePosition, AnchorPoint, ActualSize, out var selection))
            {
                ApplySelection(selection);
            }
        }
        else if (e.LeftButton == MouseButtonState.Pressed)
        {
            if (TryToCalculateSelectionForPoint(MousePosition, AnchorPoint, ActualSize, out var selection))
            {
                AdaptSelection(selection);
            }
        }
    }

    private static Point CalculatePosition(Point mousePositionAbs, Size actualSize)
    {
        var mousePosition = new Point()
        {
            X = mousePositionAbs.X.EnsureInRange(0, actualSize.Width),
            Y = mousePositionAbs.Y.EnsureInRange(0, actualSize.Height),
        };
        return mousePosition;
    }

    private static WpfRect CalculateSelectionPoint(WpfRect destinationRect, WpfRect selectionRect)
    {
        var newSelection = new Rect
        {
            X = Math.Min(destinationRect.Width - 0.01, selectionRect.X),
            Y = Math.Min(destinationRect.Height - 0.01, selectionRect.Y),
            Width = Math.Max(1, selectionRect.Width),
            Height = Math.Max(1, selectionRect.Height)
        };
        newSelection.Intersect(destinationRect);
        return newSelection;
    }
    
    private static bool TryToCalculateSelectionForPoint(Point mousePosition, Point anchorPoint, Size actualSize, out WpfRect selection)
    {
        if (anchorPoint.X < 0 || anchorPoint.Y < 0 || anchorPoint.X  > actualSize.Width || anchorPoint.Y > actualSize.Height)
        {
            return false;
        }
        var destinationRect = new Rect(new Point(0, 0), actualSize);
        selection = CalculateSelectionPoint(destinationRect, new Rect(mousePosition, new Size(1, 1)));
        return true;
    }
    
    private static bool TryToCalculateSelection(Point mousePosition, Point anchorPoint, Size actualSize, out WpfRect selection)
    {
        var destinationRect = new Rect(new Point(0, 0), actualSize);

        var topLeft = new WpfPoint(mousePosition.X < anchorPoint.X ? mousePosition.X : anchorPoint.X, mousePosition.Y < anchorPoint.Y ? mousePosition.Y : anchorPoint.Y);
        var bottomRight = new WpfPoint(mousePosition.X > anchorPoint.X ? mousePosition.X : anchorPoint.X, mousePosition.Y > anchorPoint.Y ? mousePosition.Y : anchorPoint.Y);

        var selectionRect = new WpfRect(topLeft, bottomRight);
        selectionRect.Intersect(destinationRect);

        if (selectionRect.Width <= 0 || selectionRect.Height <= 0)
        {
            return false;
        }

        selection = CalculateSelectionPoint(destinationRect, selectionRect);
        return true;
    }

    private void ApplySelection(WpfRect selection)
    {
        Selection = selection;
    }

    private void AdaptSelection(WpfRect selection)
    {
        var projectedSelection = ScreenRegionUtils.CalculateProjection(selection, ActualSize, ProjectionBounds);
        SetCurrentValue(SelectionProjectedProperty, projectedSelection);
        ApplySelection(ScreenRegionUtils.CalculateProjection(projectedSelection, ActualSize, ProjectionBounds));
    }
}