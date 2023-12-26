using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using JetBrains.Annotations;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PoeShared.UI;
using PropertyBinder;
using ReactiveUI;
using Unity;
using WindowsHook;

namespace PoeShared.RegionSelector.ViewModels;

[Obsolete("Replaced with ISelectionAdorner")]
internal sealed class SelectionAdornerLegacy : DisposableReactiveObject, ISelectionAdornerLegacy
{
    private static readonly IFluentLog Log = typeof(SelectionAdornerLegacy).PrepareLogger();
    private static readonly int CurrentProcessId = Process.GetCurrentProcess().Id;
    private static readonly Binder<SelectionAdornerLegacy> Binder = new();

    private readonly IKeyboardEventsSource keyboardEventsSource;
    private readonly IWindowTracker mainWindowTracker;

    private readonly ObservableAsPropertyHelper<bool> ownerIsVisibleSource;
    private readonly ObservableAsPropertyHelper<bool> selectionIsNotEmptySource;
    private readonly IScheduler uiDispatcher;

    static SelectionAdornerLegacy()
    {
        Binder
            .BindIf(x => x.ProjectionBounds.IsNotEmptyArea(), x => ScreenRegionUtils.CalculateProjection(
                x.Selection,
                x.RenderSize,
                x.ProjectionBounds))
            .Else(x => x.Selection.IsEmptyArea() ? WinRect.Empty : x.Selection.ToWinRectangle())
            .To((x, v) => x.ProjectedSelection = v);
            
        Binder
            .BindIf(x => x.ProjectionBounds.IsNotEmptyArea(), x => ScreenRegionUtils.CalculateProjection(
                new Rect(x.MousePosition, new Size(1, 1)),
                x.RenderSize,
                x.ProjectionBounds).Location)
            .Else(x => x.MousePosition.ToWinPoint())
            .To((x, v) => x.ProjectedMousePosition = v);
    }
        
    public SelectionAdornerLegacy(
        [NotNull] IKeyboardEventsSource keyboardEventsSource,
        [Dependency(WellKnownSchedulers.UIOverlay)] IScheduler uiScheduler,
        [NotNull] [Dependency(WellKnownWindows.MainWindow)] IWindowTracker mainWindowTracker)
    {
        this.keyboardEventsSource = keyboardEventsSource;
        this.mainWindowTracker = mainWindowTracker;
        this.uiDispatcher = uiScheduler;

        ownerIsVisibleSource = this.WhenAnyValue(x => x.Owner)
            .Select(x => x == null ? Observable.Return(false) : x.Observe(UIElement.IsVisibleProperty, x => x.IsVisible))
            .Switch()
            .ToProperty(this, x => x.OwnerIsVisible)
            .AddTo(Anchors);

        this.RaiseWhenSourceValue(x => x.RenderSize, this, x => x.Owner).AddTo(Anchors);
        this.RaiseWhenSourceValue(x => x.RenderSize, this, x => x.ScreenRenderSize).AddTo(Anchors);

        selectionIsNotEmptySource = this.WhenAnyValue(x => x.Selection)
            .Select(x => x.Size.IsNotEmpty())
            .ToProperty(this, x => x.SelectionIsNotEmpty)
            .AddTo(Anchors);
            
        Binder.Attach(this).AddTo(Anchors);
    }

    public bool SelectionIsNotEmpty => selectionIsNotEmptySource.Value;

    public bool OwnerIsVisible => ownerIsVisibleSource.Value;

    public double StrokeThickness { get; } = 2;

    public Brush Stroke { get; } = Brushes.Lime;

    /// <summary>
    /// Mouse position at the start of Select operation
    /// </summary>
    public Point AnchorPoint { get; private set; }
        
    public WinRect ProjectionBounds { get; [UsedImplicitly] set; }
        
    public WinRect ProjectedSelection { get; [UsedImplicitly] private set; }
        
    public WinPoint ProjectedMousePosition { get; [UsedImplicitly] private set; }

    public bool IsVisible { get; private set; }

    public Rect Selection { get; private set; }

    public bool StopWhenAppFocusLost { get; set; }

    public bool ShowCrosshair { get; set; }

    public bool ShowBackground { get; set; } = true;

    public double BackgroundOpacity { get; set; } = 0.5;

    public Size RenderSize => Owner?.RenderSize ?? Size.Empty;

    public WinSize ScreenRenderSize => Owner == null ? WinSize.Empty : RenderSize.ToScreen(Owner);

    public Point MousePosition { get; private set; }

    public UIElement Owner { get; set; }
        
    public IObservable<Rect> StartSelection(bool supportBoxSelection)
    {
        //FIXME Remove side-effects, code became too complicated
        return Observable.Create<Rect>(
            subscriber =>
            {
                Log.Debug($"Initializing Selection");
                if (IsVisible)
                {
                    subscriber.OnError(new InvalidOperationException("Selection is already in progress"));
                }
                IsVisible = true;
                var selectionAnchors = new CompositeDisposable();
                Disposable.Create(() => Log.Debug($"Disposing SelectionAnchors")).AddTo(selectionAnchors);
                Disposable.Create(() => IsVisible = false).AddTo(selectionAnchors);
                Selection = Rect.Empty;
                
                

                Observable.Merge(
                        mainWindowTracker.WhenAnyValue(x => x.ActiveProcessId).Where(x => StopWhenAppFocusLost).Where(x => x != CurrentProcessId).Select(x => $"main window lost focus - processId changed"),
                        keyboardEventsSource.WhenKeyDown.Where(x => x.KeyData == Keys.Escape).Do(x => x.Handled = true).Select(x => $"{x.KeyData} pressed"),
                        keyboardEventsSource.WhenMouseDown.Where(x => x.Button != MouseButtons.Left).Do(x => x.Handled = true).Select(x => $"mouse {x.Button} pressed"))
                    .Do(reason => Log.Info($"Closing SelectionAdorner, reason: {reason}, activeWindow: {mainWindowTracker.ActiveWindowTitle}, activeProcess: {mainWindowTracker.ActiveProcessId}, currentProcess: {CurrentProcessId}"))
                    .ObserveOn(uiDispatcher)
                    .SubscribeSafe(subscriber.OnCompleted, Log.HandleUiException)
                    .AddTo(selectionAnchors);
                    
                this.WhenAnyValue(x => x.Owner, x => x.OwnerIsVisible)
                    .Select(x => x.Item1 != null && x.Item2
                        ? keyboardEventsSource.WhenMouseMove
                            .Sample(UiConstants.UiAnimationDelay)
                            .StartWith(new MouseEventExtArgs(MouseButtons.None, 0, System.Windows.Forms.Cursor.Position.X, System.Windows.Forms.Cursor.Position.Y, 0))
                        : Observable.Empty<MouseEventExtArgs>())
                    .Switch()
                    .ObserveOn(uiDispatcher)
                    .Where(x => Owner is { IsVisible: true })
                    .SubscribeSafe(HandleMouseMove, Log.HandleUiException)
                    .AddTo(selectionAnchors);

                this.WhenAnyValue(x => x.Owner, x => x.OwnerIsVisible)
                    .Select(x => x.Item1 != null && x.Item2 
                        ? Observable
                            .FromEventPattern<MouseButtonEventHandler, MouseButtonEventArgs>(
                                h => x.Item1.MouseDown += h,
                                h => x.Item1.MouseDown -= h)
                            .Select(x => x.EventArgs)
                        : Observable.Empty<MouseButtonEventArgs>())
                    .Switch()
                    .Where(x => x.LeftButton == MouseButtonState.Pressed)
                    .SelectSafeOrDefault(x =>
                    {
                        var coords = x.GetPosition(Owner);
                        AnchorPoint = coords;
                        var region = new Rect(AnchorPoint.X, AnchorPoint.Y, 1, 1);
                        Selection = region;
                        return supportBoxSelection ? keyboardEventsSource.WhenMouseUp.Where(y => y.Button == MouseButtons.Left).ToUnit() : Observable.Return(Unit.Default);
                    })
                    .Switch()
                    .ObserveOn(uiDispatcher)
                    .Select(_ => Selection)
                    .SubscribeSafe(subscriber)
                    .AddTo(selectionAnchors);

                return selectionAnchors;
            });
    }

    private void UpdatePosition(Point pt)
    {
        var coords = Owner.PointFromScreen(pt);
        var renderSize = Owner.RenderSize;
        MousePosition = new Point(
            Math.Max(0, Math.Min(coords.X, renderSize.Width)),
            Math.Max(0, Math.Min(coords.Y, renderSize.Height)));
    }

    private void HandleMouseMove(MouseEventExtArgs e)
    {
        UpdatePosition(e.Location.ToWpfPoint());
        var renderSize = Owner.RenderSize;
        if (e.Button == MouseButtons.Left)
        {
            var destinationRect = new Rect(0, 0, renderSize.Width, renderSize.Height);
            var newSelection = new Rect
            {
                X = MousePosition.X < AnchorPoint.X
                    ? MousePosition.X
                    : AnchorPoint.X,
                Y = MousePosition.Y < AnchorPoint.Y
                    ? MousePosition.Y
                    : AnchorPoint.Y,
                Width = Math.Max(1, Math.Abs(MousePosition.X - AnchorPoint.X)),
                Height = Math.Max(1, Math.Abs(MousePosition.Y - AnchorPoint.Y))
            };
            newSelection.Intersect(destinationRect);
            Selection = newSelection;
        }
        else
        {
            Selection = new Rect(MousePosition, Size.Empty);
        }
    }
}