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
using JetBrains.Annotations;
using log4net;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using ReactiveUI;
using Unity;
using WindowsHook;
using WinSize = System.Drawing.Size;
using WinPoint = System.Drawing.Point;
using WinRectangle = System.Drawing.Rectangle;

namespace PoeShared.RegionSelector.ViewModels
{
    internal sealed class SelectionAdornerViewModel : DisposableReactiveObject, ISelectionAdornerViewModel
    {
        private static readonly IFluentLog Log = typeof(SelectionAdornerViewModel).PrepareLogger();
        private static readonly int CurrentProcessId = Process.GetCurrentProcess().Id;

        private readonly IKeyboardEventsSource keyboardEventsSource;
        private readonly IWindowTracker mainWindowTracker;
        private readonly IScheduler uiScheduler;

        private readonly ObservableAsPropertyHelper<bool> OwnerIsVisibleSource;
        private readonly ObservableAsPropertyHelper<bool> selectionIsNotEmptySource;

        public SelectionAdornerViewModel(
            [NotNull] IKeyboardEventsSource keyboardEventsSource,
            [NotNull] [Dependency(WellKnownWindows.MainWindow)] IWindowTracker mainWindowTracker,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            this.keyboardEventsSource = keyboardEventsSource;
            this.mainWindowTracker = mainWindowTracker;
            this.uiScheduler = uiScheduler;

            OwnerIsVisibleSource = this.WhenAnyValue(x => x.Owner)
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

            Observable.CombineLatest(
                    this.WhenAnyValue(x => x.ScreenSelection).ToUnit(), 
                    this.WhenAnyValue(x => x.MousePosition).ToUnit(), 
                    this.WhenAnyValue(x => x.OwnerIsVisible).ToUnit(), 
                    this.WhenAnyValue(x => x.Owner).ToUnit())
                .SubscribeSafe(x =>
                {
                    if (Owner == null || !OwnerIsVisible)
                    {
                        return;
                    }
                    var topLeft = Owner.PointToScreen(new Point(0, 0));
                    var absoluteMousePosition = Owner.PointToScreen(MousePosition);
                    absoluteMousePosition.Offset(-topLeft.X, -topLeft.Y);
                    ScreenMousePosition = absoluteMousePosition.ToWinPoint();
                    ScreenSelection = Selection.ScaleToScreen();
                }, Log.HandleUiException)
                .AddTo(Anchors);
        }

        public Rect Selection { get; private set; }

        public WinRectangle ScreenSelection { get; private set; }

        public bool SelectionIsNotEmpty => selectionIsNotEmptySource.Value;

        public bool OwnerIsVisible => OwnerIsVisibleSource.Value;

        public bool StopWhenFocusLost { get; set; }
        
        public bool ShowCrosshair { get; set; } = false;

        public bool ShowBackground { get; set; } = true;

        public double BackgroundOpacity { get; set; } = 0.5;

        public double StrokeThickness { get; } = 2;

        public Brush Stroke { get; } = Brushes.Lime;
        
        public Size RenderSize => Owner?.RenderSize ?? Size.Empty;
        
        public WinSize ScreenRenderSize => Owner == null ? WinSize.Empty : RenderSize.ToScreen(Owner);

        public Point AnchorPoint { get; private set; }

        public Point MousePosition { get; private set; }

        public WinPoint ScreenMousePosition { get; private set; }

        public UIElement Owner { get; set; }

        public bool IsVisible { get; private set; }

        public IObservable<Rect> StartSelection()
        {
            //FIXME Remove side-effects, code became too complicated
            return Observable.Create<Rect>(
                subscriber =>
                {
                    Log.Debug($"Initializing Selection");
                    IsVisible = true;
                    var selectionAnchors = new CompositeDisposable();
                    Disposable.Create(() => Log.Debug($"Disposing SelectionAnchors")).AddTo(selectionAnchors);
                    Disposable.Create(() => IsVisible = false).AddTo(selectionAnchors);
                    Selection = Rect.Empty;

                    Observable.Merge(
                            mainWindowTracker.WhenAnyValue(x => x.ActiveProcessId).Where(x => StopWhenFocusLost).Where(x => x != CurrentProcessId).Select(x => $"main window lost focus - processId changed"),
                            keyboardEventsSource.WhenKeyUp.Where(x => x.KeyData == Keys.Escape).Select(x => $"{x.KeyData} pressed"),
                            keyboardEventsSource.WhenMouseUp.Where(x => x.Button != MouseButtons.Left).Select(x => $"mouse {x.Button} pressed"))
                        .Do(reason => Log.Info($"Closing SelectionAdorner, reason: {reason}, activeWindow: {mainWindowTracker.ActiveWindowTitle}, activeProcess: {mainWindowTracker.ActiveProcessId}, currentProcess: {CurrentProcessId}"))
                        .ObserveOn(uiScheduler)
                        .SubscribeSafe(subscriber.OnCompleted, Log.HandleUiException)
                        .AddTo(selectionAnchors);
                    
                    this.WhenAnyValue(x => x.Owner, x => x.OwnerIsVisible)
                        .Select(x => x.Item1 != null && x.Item2
                                ? keyboardEventsSource.WhenMouseMove
                                    .StartWith(new MouseEventExtArgs(MouseButtons.None, 0, System.Windows.Forms.Cursor.Position.X, System.Windows.Forms.Cursor.Position.Y, 0))
                                    : Observable.Empty<MouseEventExtArgs>())
                        .Switch()
                        .ObserveOn(uiScheduler)
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
                            return keyboardEventsSource.WhenMouseUp.Where(y => y.Button == MouseButtons.Left).ToUnit();
                        })
                        .Switch()
                        .ObserveOn(uiScheduler)
                        .Select(
                            _ =>
                            {
                                var selectedRegion = Selection;
                                Selection = Rect.Empty;
                                return selectedRegion;
                            })
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
                Selection = new Rect(AnchorPoint, Size.Empty);
            }
        }
    }
}