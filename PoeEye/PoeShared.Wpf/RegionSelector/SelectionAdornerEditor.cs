using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using App.Metrics;
using JetBrains.Annotations;
using PoeShared.Native;
using PoeShared.Scaffolding;
using PoeShared.UI;
using PropertyBinder;
using ReactiveUI;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace PoeShared.RegionSelector;

[TemplatePart(Name = PART_Canvas, Type = typeof(Canvas))]
public class SelectionAdornerEditor : ReactiveControl
{
    private const string PART_Canvas = "PART_Canvas";

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
        nameof(ProjectionBounds), typeof(WinRect), typeof(SelectionAdornerEditor), new FrameworkPropertyMetadata(default(WinRect)));

    public static readonly DependencyProperty SelectionProjectedProperty = DependencyProperty.Register(
        nameof(SelectionProjected), typeof(WinRect), typeof(SelectionAdornerEditor), new FrameworkPropertyMetadata(default(WinRect), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty MousePositionProjectedProperty = DependencyProperty.Register(
        nameof(MousePositionProjected), typeof(WinPoint), typeof(SelectionAdornerEditor), new FrameworkPropertyMetadata(default(WinPoint), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty IsBoxSelectionEnabledProperty = DependencyProperty.Register(
        nameof(IsBoxSelectionEnabled), typeof(bool), typeof(SelectionAdornerEditor), new FrameworkPropertyMetadata(true));

    public static readonly DependencyProperty ViewTransformProperty = DependencyProperty.Register(
        nameof(ViewTransform), typeof(Matrix), typeof(SelectionAdornerEditor), new PropertyMetadata(default(Matrix)));

    private Matrix localToWorld = Matrix.Identity;
    private Matrix worldToLocal = Matrix.Identity;
    private Matrix localToView = Matrix.Identity;
    private Matrix viewToLocal = Matrix.Identity;

    static SelectionAdornerEditor()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(SelectionAdornerEditor), new FrameworkPropertyMetadata(typeof(SelectionAdornerEditor)));

        Binder.Bind(x => CalculateWorldToLocalTransform(x.LocalRect.Size, x.ProjectionBounds))
            .To((x, v) =>
            {
                x.localToWorld = v;
                x.worldToLocal = v.InverseOrIdentity();
            });

        Binder.Bind(x => x.ViewTransform)
            .To((x, v) =>
            {
                x.localToView = v;
                x.viewToLocal = v.InverseOrIdentity();
            });

        Binder.Bind(x => x.localToView.Transform(x.worldToLocal.Transform(x.SelectionProjected.ToWpfRectangle())))
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

    public Matrix ViewTransform
    {
        get => (Matrix) GetValue(ViewTransformProperty);
        set => SetValue(ViewTransformProperty, value);
    }

    public WpfRect Selection { get; private set; }

    public WpfPoint MousePosition { get; private set; }

    public WpfPoint AnchorPoint { get; private set; }

    private WpfRect LocalRect { get; set; }

    public WinRect SelectionProjectedTemp { get; private set; }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        SizeChanged += CanvasOnSizeChanged;

        var mouseEvents = Observable.Using(() =>
        {
            MouseMove += OnMouseMove;
            PreviewMouseDown += OnPreviewMouseDown;
            PreviewMouseUp += OnPreviewMouseUp;
            PreviewKeyDown += OnPreviewKeyDown;
            Focus();
            CaptureMouse();

            return Disposable.Create(() =>
            {
               MouseMove -= OnMouseMove;
               PreviewMouseDown -= OnPreviewMouseDown;
               PreviewMouseUp -= OnPreviewMouseUp;
               PreviewKeyDown -= OnPreviewKeyDown;
               ReleaseMouseCapture();
            });
        }, disposable => Observable.Never<Unit>());

        var isInEditModeSource = this.WhenAnyValue(x => x.IsInEditMode);

        var keyboardFocusLost = this.Observe(IsKeyboardFocusWithinProperty, x => x.IsKeyboardFocusWithin)
            .Where(x => x == false)
            .Select(x => true);

        isInEditModeSource
            .Select(x => x ? mouseEvents : Observable.Empty<Unit>())
            .Switch()
            .Subscribe()
            .AddTo(Anchors);

        isInEditModeSource
            .Select(x => x ? keyboardFocusLost : Observable.Empty<bool>())
            .Switch()
            .Subscribe(Reset)
            .AddTo(Anchors);

        this.WhenAnyValue(x => x.IsInEditMode)
            .Where(x => x == true)
            .Subscribe(() =>
            {
                AnchorPoint = default;
                Selection = default;
                SelectionProjectedTemp = default;
            })
            .AddTo(Anchors);
    }

    protected override void OnContextMenuOpening(ContextMenuEventArgs e)
    {
        e.Handled = true;
    }

    private void CanvasOnSizeChanged(object sender, EventArgs e)
    {
        LocalRect = new WpfRect(new WpfPoint(0, 0), new WpfSize(ActualWidth, ActualHeight));
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Reset();
        }
    }

    private void OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (AnchorPoint.IsEmpty())
        {
            return;
        }

        try
        {
            switch (e.ChangedButton)
            {
                case MouseButton.Left:
                    e.Handled = true;
                    
                    UpdateSelection();
                    SetCurrentValue(SelectionProjectedProperty, SelectionProjectedTemp);
                    break;
                case MouseButton.Right:
                    e.Handled = true;
                    
                    Reset();
                    break;
            }
        }
        finally
        {
            AnchorPoint = default;
        }
    }

    private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        switch (e.ChangedButton)
        {
            case MouseButton.Left:
                e.Handled = true;

                AnchorPoint = GetMousePosition(e);
                UpdateSelection();
                if (IsBoxSelectionEnabled == false) 
                {
                    SetCurrentValue(SelectionProjectedProperty, SelectionProjectedTemp);
                }
                break;
            case MouseButton.Right:
                e.Handled = true;

                Reset();
                break;
        }

    }

    private WpfPoint GetMousePosition(MouseEventArgs e)
    {
        var localMousePos = e.GetPosition(this);

        var viewportRect = localToView.Transform(LocalRect);
        return localMousePos.EnsureInBounds(viewportRect);
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        MousePosition = GetMousePosition(e);
        UpdateMousePosition();
        
        if (AnchorPoint.IsEmpty())
        {
            //nothing to do without anchor point
            return;
        }
        UpdateSelection();
    }

    private void UpdateMousePosition()
    {
        var localMousePos = viewToLocal.Transform(MousePosition);
        var worldMousePos = localToWorld.Transform(localMousePos);
        MousePositionProjected = worldMousePos.IsEmpty() ? WinPoint.Empty : worldMousePos.ToWinPoint();
    }

    private void UpdateSelection()
    {
        var localSelection = CalculateSelection(MousePosition, AnchorPoint, LocalRect.Size);
        
        var worldSelection = localSelection.IsEmpty
            ? WpfRect.Empty
            : localToWorld.Transform(localSelection);
        Selection = worldToLocal.Transform(worldSelection);

        var localViewSelection =  localSelection.IsEmpty 
            ? WpfRect.Empty 
            : viewToLocal.Transform(localSelection);
        var worldViewSelection = localViewSelection.IsEmpty
            ? WpfRect.Empty
            : localToWorld.Transform(localViewSelection);
        SelectionProjectedTemp = ToWinRegion(worldViewSelection);
    }

    private void Reset()
    {
        AnchorPoint = default;
        IsInEditMode = false;
    }

    private static WinRect ToWinRegion(WpfRect rect)
    {
        if (rect.IsEmpty)
        {
            return WinRect.Empty;
        }

        var baseRect = new WinRect
        {
            X = (int)Math.Floor(rect.X),
            Y = (int)Math.Floor(rect.Y),
            Width = (int)Math.Floor(rect.Width),
            Height = (int)Math.Floor(rect.Height)
        };
        var leftovers = new WpfRect
        {
            X = rect.X - baseRect.X,
            Y = rect.Y - baseRect.Y,
            Width = rect.Width - baseRect.Width,
            Height = rect.Height - baseRect.Height
        };

        return new WinRect
        {
            X = baseRect.X + (int)Math.Round(leftovers.X),
            Y = baseRect.Y + (int)Math.Round(leftovers.Y),
            Width = Math.Max(1, baseRect.Width + (leftovers.Width > 0.2 ? 1 : 0)),
            Height = Math.Max(1, baseRect.Height + (leftovers.Height > 0.2 ? 1 : 0)),
        };
    }

    private static WpfRect CalculateSelection(WpfPoint mousePosition, WpfPoint anchorPoint, WpfSize actualSize)
    {
        var topLeft = new WpfPoint(mousePosition.X < anchorPoint.X ? mousePosition.X : anchorPoint.X, mousePosition.Y < anchorPoint.Y ? mousePosition.Y : anchorPoint.Y);
        var bottomRight = new WpfPoint(mousePosition.X > anchorPoint.X ? mousePosition.X : anchorPoint.X, mousePosition.Y > anchorPoint.Y ? mousePosition.Y : anchorPoint.Y);

        var destinationRect = new WpfRect(new WpfPoint(0, 0), actualSize);
        var selectionRect = new WpfRect(topLeft, bottomRight);
        selectionRect.Intersect(destinationRect);

        return selectionRect;
    }

    private static Matrix CalculateWorldToLocalTransform(WpfSize actualSize, WinRect projectionBounds)
    {
        if (actualSize.IsEmpty || projectionBounds.IsEmpty)
        {
            return Matrix.Identity;
        }

        var scaleX = projectionBounds.Width / actualSize.Width;
        var scaleY = projectionBounds.Height / actualSize.Height;

        var wpfToObjectMatrix = new Matrix(scaleX, 0, 0, scaleY, projectionBounds.X, projectionBounds.Y);
        return wpfToObjectMatrix;
    }
}