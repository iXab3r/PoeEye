using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows;
using PoeShared.Native;
using PoeShared.Scaffolding;
using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using System.Windows.Threading;
using JetBrains.Annotations;
using PoeShared.Prism;
using PoeShared.UI;
using PoeShared.WindowSeekers;
using ReactiveUI;
using Unity;

namespace PoeShared.RegionSelector.ViewModels;

[Obsolete("Should net be used as it is way worse than WindowFinder")]
internal sealed class WindowRegionSelector : OverlayViewModelBase, IWindowRegionSelector
{
    private static readonly double MinSelectionArea = 20;
    
    private readonly TaskWindowSeeker windowSeeker;

    public WindowRegionSelector(
        IFactory<TaskWindowSeeker> taskWindowSeekerFactory,
        [Dependency(WellKnownSchedulers.UIOverlay)] IScheduler overlayScheduler,
        [NotNull] ISelectionAdornerLegacy selectionAdorner)
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
            .ObserveOn(overlayScheduler)
            .Select(x => new WinRect(x.X, x.Y, 1, 1))
            .Select(ToRegionResult)
            .Do(x => Log.Debug($"Selection candidate: {x}"))
            .SubscribeSafe(x => SelectionCandidate = x, Log.HandleUiException)
            .AddTo(Anchors);
            
        refreshRequest
            .SubscribeSafe(() => windowSeeker.Refresh(), Log.HandleUiException)
            .AddTo(Anchors);
            
        this.WhenAnyValue(x => x.IsBusy)
            .Select(x => x ? Observables.BlockingTimer( TimeSpan.FromSeconds(1)).ObserveOn(overlayScheduler).ToUnit() : Observable.Empty<Unit>())
            .Switch()
            .SubscribeSafe(refreshRequest)
            .AddTo(Anchors);

        this.WhenAnyValue(x => x.IsBusy)
            .CombineLatest(selectionAdorner.WhenAnyValue(x => x.Owner), (busy, owner) => new { busy, owner })
            .ObserveOn(overlayScheduler)
            .Select(x => x.busy && x.owner != null ? x.owner.Dispatcher.Invoke(x.owner.FindVisualAncestor<Window>) : null)
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
    }

    public ISelectionAdornerLegacy SelectionAdorner { get; }

    public bool IsBusy { get; private set; }

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
                    var selection = new WpfRect(SelectionAdorner.Owner.PointToScreen(x.Location), x.Size).ToWinRectangle();
                    if (selection.Width >= minSelection.Width && selection.Height >= minSelection.Height)
                    {
                        Log.Debug($"Selected region: {x} (screen: {selection}) (min size: {minSelection})");
                        return selection;
                    }

                    var result = selection with {Width = 0, Height = 0};
                    Log.Debug($"Selected region({x}, screen: {selection}) is less than required({minSelection}, converting selection {selection} to {result}");
                    return result;
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

    private RegionSelectorResult ToRegionResult(WinRect screenRegion)
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
            absoluteSelection.Offset(window.ClientRect.Left, window.ClientRect.Top);
            Log.Debug($"Found a window using region {screenRegion}: {window}");
            
            var frameRect = window.DwmFrameBounds.ToWpfRectangle();
            var topLeft = SelectionAdorner.Owner.PointFromScreen(frameRect.Location);
            var bottomRight = SelectionAdorner.Owner.PointFromScreen(new WpfPoint(frameRect.Location.X + frameRect.Width, frameRect.Location.Y + frameRect.Height));
            var wpfBounds = new WpfRect
            {
                X = topLeft.X,
                Y = topLeft.Y,
                Width = bottomRight.X - topLeft.X,
                Height = bottomRight.Y - topLeft.Y
            };
            
            return new RegionSelectorResult
            {
                AbsoluteSelection = absoluteSelection,
                Selection = selection,
                Window = window,
                Reason = "OK",
                WindowBounds = wpfBounds
            };
        }

        Log.Warn($"Failed to find window in region {screenRegion}");
        return new RegionSelectorResult { Reason = $"Could not find matching window in region {screenRegion}" };
    }

    private static (IWindowHandle window, WinRect selection) FindMatchingWindow(WinRect selection, IEnumerable<IWindowHandle> windows)
    {
        var topLeft = new WinPoint(selection.Left, selection.Top);
        var intersections = windows
            .Where(x => x.ProcessId != Environment.ProcessId)
            .Where(x => UnsafeNative.WindowIsVisible(x.Handle))
            .Where(x => x.ClientRect.IsNotEmptyArea())
            .Where(x => x.ClientRect.Contains(topLeft))
            .Select(
                (x, _) =>
                {
                    WinRect intersection;
                    if (selection is {Width: > 0, Height: > 0})
                    {
                        intersection = x.ClientRect;
                        intersection.Intersect(selection);
                        intersection.Offset(-x.ClientRect.Left, -x.ClientRect.Top);
                    }
                    else
                    {
                        intersection = x.ClientRect with {X = 0, Y = 0};
                    }
                        
                    return new
                    {
                        Window = x,
                        Intersection = intersection,
                        Area = intersection.Width * intersection.Height
                    };
                })
            .Where(x => x.Intersection.IsNotEmptyArea())
            .OrderBy(x => x.Window.ZOrder)
            .ToArray();

        var topmostHandle = UnsafeNative.GetTopmostHwnd(intersections.Select(x => x.Window.Handle).ToArray());
        var result = intersections.FirstOrDefault(x => x.Window.Handle == topmostHandle);

        return result == null
            ? (null, WinRect.Empty)
            : (result.Window, result.Intersection);
    }
}