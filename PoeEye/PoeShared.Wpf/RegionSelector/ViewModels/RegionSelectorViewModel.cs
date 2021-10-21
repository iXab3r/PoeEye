using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows;
using PoeShared.Native;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PoeShared.Prism;
using PoeShared.UI;
using PoeShared.WindowSeekers;
using ReactiveUI;
using Unity;
using Point = System.Drawing.Point;
using WinSize = System.Drawing.Size;
using WinPoint = System.Drawing.Point;
using WinRectangle = System.Drawing.Rectangle;

namespace PoeShared.RegionSelector.ViewModels
{
    internal sealed class RegionSelectorViewModel : OverlayViewModelBase, IRegionSelectorViewModel
    {
        private static readonly IFluentLog Log = typeof(RegionSelectorViewModel).PrepareLogger();
        private static readonly TimeSpan ThrottlingPeriod = TimeSpan.FromMilliseconds(250);
        private static readonly int CurrentProcessId = Process.GetCurrentProcess().Id;
        private static readonly double MinSelectionArea = 20;
        private readonly TaskWindowSeeker windowSeeker;

        public RegionSelectorViewModel(
            IFactory<TaskWindowSeeker> taskWindowSeekerFactory,
            [NotNull] ISelectionAdornerViewModel selectionAdorner,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            Title = "Region Selector";
            OverlayMode = OverlayMode.Layered;
            IsUnlockable = false;
            EnableHeader = false;
            SizeToContent = SizeToContent.Manual;

            SelectionAdorner = selectionAdorner.AddTo(Anchors);
            windowSeeker = taskWindowSeekerFactory.Create();
            windowSeeker.SkipNotVisibleWindows = true;

            var refreshRequest = new Subject<Unit>();

            SelectionAdorner.WhenAnyValue(x => x.MousePosition, x => x.Owner).ToUnit()
                .Merge(refreshRequest)
                .Select(x => new { SelectionAdorner.MousePosition, SelectionAdorner.Owner })
                .Where(x => x.Owner != null)
                .Select(x => x.MousePosition.ToScreen(x.Owner))
                .Sample(UiConstants.UiThrottlingDelay)
                .ObserveOn(uiScheduler)
                .Select(x => new Rectangle(x.X, x.Y, 1, 1))
                .Select(ToRegionResult)
                .Do(x => Log.Debug($"Selection candidate: {x}"))
                .SubscribeSafe(x => SelectionCandidate = x, Log.HandleUiException)
                .AddTo(Anchors);
            
            refreshRequest
                .SubscribeSafe(() => windowSeeker.Refresh(), Log.HandleUiException)
                .AddTo(Anchors);
            
            this.WhenAnyValue(x => x.IsBusy)
                .Select(x => x ? Observable.Timer(DateTimeOffset.Now, TimeSpan.FromSeconds(1), uiScheduler).ToUnit() : Observable.Empty<Unit>())
                .Switch()
                .SubscribeSafe(refreshRequest)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.IsBusy)
                .CombineLatest(selectionAdorner.WhenAnyValue(x => x.Owner), (busy, owner) => new { busy, owner })
                .ObserveOn(uiScheduler)
                .Select(x => x.busy && x.owner != null ? x.owner.FindVisualAncestor<Window>() : null)
                .Select(x => x != null ? 
                        Observable.Merge( 
                            Observable.FromEventPattern<RoutedEventHandler, RoutedEventArgs>(h => x.LostFocus += h, h => x.LostFocus -= h).Select(x => "window LostFocus"),
                            Observable.FromEventPattern<EventHandler, EventArgs>(h => x.Deactivated += h, h => x.Deactivated -= h).Select(x => "window Deactivated"))
                        : Observable.Empty<string>())
                .Switch()
                .SubscribeSafe(reason =>
                {
                    Log.Debug($"Stopping selection, reason: {reason}");
                    IsBusy = false;
                }, Log.HandleUiException)
                .AddTo(Anchors);
            
            this.WhenAnyValue(x => x.SelectionCandidate)
                .SubscribeSafe(
                    regionResult =>
                    {
                        if (regionResult == null || !regionResult.IsValid)
                        {
                            SelectionCandidateBounds = Rect.Empty;
                            return;
                        } 
                        
                        var bounds = regionResult.Window.WindowBounds.ScaleToWpf();
                        var relative = SelectionAdorner.Owner.PointFromScreen(bounds.Location);
                        SelectionCandidateBounds = new Rect(relative, bounds.Size);
                    }, Log.HandleUiException)
                .AddTo(Anchors);
        }

        public ISelectionAdornerViewModel SelectionAdorner { get; }

        public bool IsBusy { get; private set; }

        public Rect SelectionCandidateBounds { get; private set; }

        public RegionSelectorResult SelectionCandidate { get; private set; }

        public async Task<RegionSelectorResult> StartSelection(WinSize minSelection)
        {
            try
            {
                if (IsBusy)
                {
                    IsBusy = false;
                }
                IsBusy = true;
                
                var result = await SelectionAdorner.StartSelection()
                    .TakeUntil(this.WhenAnyValue(x => x.IsBusy).Where(x => x == false))
                    .Select(x =>
                    {
                        var selection = new Rect(SelectionAdorner.Owner.PointToScreen(x.Location), x.Size).ToWinRectangle();
                        if (selection.Width >= minSelection.Width && selection.Height >= minSelection.Height)
                        {
                            Log.Debug($"Selected region: {x} (screen: {selection}) (min size: {minSelection})");
                            return selection;
                        }
                        else
                        {
                            var result = new WinRectangle(selection.X, selection.Y, 0, 0);
                            Log.Debug($"Selected region({x}, screen: {selection}) is less than required({minSelection}, converting selection {selection} to {result}");
                            return result;
                        }
                    })
                    .Select(ToRegionResult)
                    .Do(x => Log.Debug($"Selection Result: {x}"))
                    .Take(1)
                    .ToTask();
                return result;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private RegionSelectorResult ToRegionResult(Rectangle screenRegion)
        {
            if (screenRegion.IsEmpty)
            {
                return new RegionSelectorResult { Reason = "Selected Empty screen region" };
            }
            
            Log.Debug($"Looking up foreground window in region {screenRegion}");
            var (window, selection) = FindMatchingWindow(screenRegion, windowSeeker.Windows);

            if (window != null)
            {
                var absoluteSelection = selection;
                absoluteSelection.Offset(window.ClientBounds.Left, window.ClientBounds.Top);
                Log.Debug($"Found a window using region {screenRegion}: {window}");
                return new RegionSelectorResult
                {
                    AbsoluteSelection = absoluteSelection,
                    Selection = selection,
                    Window = window,
                    Reason = "OK"
                };
            }

            Log.Warn($"Failed to find window in region {screenRegion}");
            return new RegionSelectorResult { Reason = $"Could not find matching window in region {screenRegion}" };
        }

        private static (IWindowHandle window, Rectangle selection) FindMatchingWindow(Rectangle selection, IEnumerable<IWindowHandle> windows)
        {
            var topLeft = new Point(selection.Left, selection.Top);
            var intersections = windows
                .Where(x => x.ProcessId != CurrentProcessId)
                .Where(x => UnsafeNative.WindowIsVisible(x.Handle))
                .Where(x => x.ClientBounds.IsNotEmpty())
                .Where(x => x.ClientBounds.Contains(topLeft))
                .Select(
                    (x, idx) =>
                    {
                        Rectangle intersection;
                        if (selection.Width > 0 && selection.Height > 0)
                        {
                            intersection = x.ClientBounds;
                            intersection.Intersect(selection);
                            intersection.Offset(-x.ClientBounds.Left, -x.ClientBounds.Top);
                        }
                        else
                        {
                            intersection = new Rectangle(0, 0, x.ClientBounds.Width, x.ClientBounds.Height);
                        }
                        
                        return new
                        {
                            Window = x,
                            Intersection = intersection,
                            Area = intersection.Width * intersection.Height
                        };
                    })
                .Where(x => GeometryExtensions.IsNotEmpty(x.Intersection))
                .OrderBy(x => x.Window.ZOrder)
                .ToArray();

            var topmostHandle = UnsafeNative.GetTopmostHwnd(intersections.Select(x => x.Window.Handle).ToArray());
            var result = intersections.FirstOrDefault(x => x.Window.Handle == topmostHandle);

            return result == null
                ? (null, Rectangle.Empty)
                : (result.Window, result.Intersection);
        }
    }
}