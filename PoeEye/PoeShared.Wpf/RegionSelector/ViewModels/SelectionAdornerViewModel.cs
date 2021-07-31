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
using Control = System.Windows.Forms.Control;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;
using WinSize = System.Drawing.Size;
using WinPoint = System.Drawing.Point;
using WinRectangle = System.Drawing.Rectangle;

namespace PoeShared.RegionSelector.ViewModels
{
    internal sealed class SelectionAdornerViewModel : DisposableReactiveObject, ISelectionAdornerViewModel
    {
        private static readonly IFluentLog Log = typeof(SelectionAdornerViewModel).PrepareLogger();
        private static readonly TimeSpan MousePositionCaptureInterval = TimeSpan.FromMilliseconds(1000 / 60f);
        private static readonly int CurrentProcessId = Process.GetCurrentProcess().Id;

        private readonly IKeyboardEventsSource keyboardEventsSource;
        private readonly IWindowTracker mainWindowTracker;
        private readonly IScheduler uiScheduler;
        private readonly IScheduler bgScheduler;

        private readonly ObservableAsPropertyHelper<bool> ownerIsVisibleSource;
        private readonly ObservableAsPropertyHelper<bool> selectionIsNotEmptySource;

        private Point anchorPoint;
        private Point mousePosition;
        private Rect selection;
        private UIElement owner;
        private bool stopWhenFocusLost;
        private WinPoint screenMousePosition;
        private WinRectangle screenSelection;
        private bool isVisible;
        private bool showCrosshair = false;
        private bool showBackground = true;
        private double backgroundOpacity = 0.5;

        public SelectionAdornerViewModel(
            [NotNull] IKeyboardEventsSource keyboardEventsSource,
            [NotNull] [Dependency(WellKnownWindows.MainWindow)] IWindowTracker mainWindowTracker,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler)
        {
            this.keyboardEventsSource = keyboardEventsSource;
            this.mainWindowTracker = mainWindowTracker;
            this.uiScheduler = uiScheduler;
            this.bgScheduler = bgScheduler;
            
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

            Observable.CombineLatest(
                    this.WhenAnyValue(x => x.ScreenSelection).ToUnit(), 
                    this.WhenAnyValue(x => x.MousePosition).ToUnit(), 
                    this.WhenAnyValue(x => x.OwnerIsVisible).ToUnit(), 
                    this.WhenAnyValue(x => x.Owner).ToUnit())
                .SubscribeSafe(x =>
                {
                    if (owner == null || !OwnerIsVisible)
                    {
                        return;
                    }
                    var topLeft = owner.PointToScreen(new Point(0, 0));
                    var absoluteMousePosition = owner.PointToScreen(mousePosition);
                    absoluteMousePosition.Offset(-topLeft.X, -topLeft.Y);
                    ScreenMousePosition = absoluteMousePosition.ToWinPoint();
                    ScreenSelection = selection.ScaleToScreen();
                }, Log.HandleUiException)
                .AddTo(Anchors);
        }

        public Rect Selection
        {
            get => selection;
            private set => RaiseAndSetIfChanged(ref selection, value);
        }

        public WinRectangle ScreenSelection
        {
            get => screenSelection;
            private set => RaiseAndSetIfChanged(ref screenSelection, value);
        }

        public bool SelectionIsNotEmpty => selectionIsNotEmptySource.Value;

        public bool OwnerIsVisible => ownerIsVisibleSource.Value;

        public bool StopWhenFocusLost
        {
            get => stopWhenFocusLost;
            set => this.RaiseAndSetIfChanged(ref stopWhenFocusLost, value);
        }
        
        public bool ShowCrosshair
        {
            get => showCrosshair;
            set => RaiseAndSetIfChanged(ref showCrosshair, value);
        }

        public bool ShowBackground
        {
            get => showBackground;
            set => RaiseAndSetIfChanged(ref showBackground, value);
        }

        public double BackgroundOpacity
        {
            get => backgroundOpacity;
            set => RaiseAndSetIfChanged(ref backgroundOpacity, value);
        }

        public double StrokeThickness { get; } = 2;

        public Brush Stroke { get; } = Brushes.Lime;
        
        public Size RenderSize => owner?.RenderSize ?? Size.Empty;
        
        public WinSize ScreenRenderSize => owner == null ? WinSize.Empty : RenderSize.ToScreen(owner);

        public Point AnchorPoint
        {
            get => anchorPoint;
            private set => RaiseAndSetIfChanged(ref anchorPoint, value);
        }

        public Point MousePosition
        {
            get => mousePosition;
            private set => RaiseAndSetIfChanged(ref mousePosition, value);
        }

        public WinPoint ScreenMousePosition
        {
            get => screenMousePosition;
            private set => RaiseAndSetIfChanged(ref screenMousePosition, value);
        }

        public UIElement Owner
        {
            get => owner;
            set => RaiseAndSetIfChanged(ref owner, value);
        }

        public bool IsVisible
        {
            get => isVisible;
            private set => RaiseAndSetIfChanged(ref isVisible, value);
        }

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
                    MousePosition = owner.PointFromScreen(Control.MousePosition.ToWpfPoint());

                    Observable.Merge(
                            mainWindowTracker.WhenAnyValue(x => x.ActiveProcessId).Where(x => StopWhenFocusLost).Where(x => x != CurrentProcessId).Select(x => $"main window lost focus - processId changed"),
                            keyboardEventsSource.WhenKeyUp.Where(x => x.KeyData == Keys.Escape).Select(x => $"{x.KeyData} pressed"),
                            keyboardEventsSource.WhenMouseUp.Where(x => x.Button != MouseButtons.Left).Select(x => $"mouse {x.Button} pressed"))
                        .Do(reason => Log.Info($"Closing SelectionAdorner, reason: {reason}, activeWindow: {mainWindowTracker.ActiveWindowTitle}, activeProcess: {mainWindowTracker.ActiveProcessId}, currentProcess: {CurrentProcessId}"))
                        .ObserveOn(uiScheduler)
                        .SubscribeSafe(subscriber.OnCompleted, Log.HandleUiException)
                        .AddTo(selectionAnchors);
                    
                    keyboardEventsSource.WhenMouseMove
                        .Where(x => OwnerIsVisible)
                        .ObserveOn(uiScheduler)
                        .SubscribeSafe(HandleMouseMove, Log.HandleUiException)
                        .AddTo(selectionAnchors);

                    Observable
                        .FromEventPattern<MouseButtonEventHandler, MouseButtonEventArgs>(
                            h => owner.MouseDown += h,
                            h => owner.MouseDown -= h)
                        .Select(x => x.EventArgs)
                        .Where(x => x.LeftButton == MouseButtonState.Pressed)
                        .Select(x =>
                        {
                            var coords = x.GetPosition(owner);
                            AnchorPoint = coords;
                            var region = new Rect(anchorPoint.X, anchorPoint.Y, 1, 1);
                            Selection = region;
                            return keyboardEventsSource.WhenMouseUp.Where(y => y.Button == MouseButtons.Left).ObserveOn(uiScheduler);
                        })
                        .Switch()
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
            var coords = owner.PointFromScreen(pt);
            var renderSize = owner.RenderSize;
            MousePosition = new Point(
                Math.Max(0, Math.Min(coords.X, renderSize.Width)),
                Math.Max(0, Math.Min(coords.Y, renderSize.Height)));
        }

        private void HandleMouseMove(MouseEventArgs e)
        {
            UpdatePosition(e.Location.ToWpfPoint());
            var renderSize = owner.RenderSize;
            if (e.Button == MouseButtons.Left)
            {
                var destinationRect = new Rect(0, 0, renderSize.Width, renderSize.Height);

                var newSelection = new Rect
                {
                    X = mousePosition.X < anchorPoint.X
                        ? mousePosition.X
                        : anchorPoint.X,
                    Y = mousePosition.Y < anchorPoint.Y
                        ? mousePosition.Y
                        : anchorPoint.Y,
                    Width = Math.Max(1, Math.Abs(mousePosition.X - anchorPoint.X)),
                    Height = Math.Max(1, Math.Abs(mousePosition.Y - anchorPoint.Y))
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