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
using JetBrains.Annotations;
using log4net;
using PoeShared;
using PoeShared.Prism;
using PoeShared.RegionSelector;
using PoeShared.RegionSelector.ViewModels;
using PoeShared.WindowSeekers;
using ReactiveUI;
using Unity;
using Point = System.Drawing.Point;

using WinSize = System.Drawing.Size;
using WinPoint = System.Drawing.Point;
using WinRectangle = System.Drawing.Rectangle;

namespace PoeShared.RegionSelector.ViewModels
{
    internal sealed class RegionSelectorViewModel : DisposableReactiveObject, IRegionSelectorViewModel
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RegionSelectorViewModel));
        private static readonly TimeSpan ThrottlingPeriod = TimeSpan.FromMilliseconds(250);
        private static readonly int CurrentProcessId = Process.GetCurrentProcess().Id;
        private static readonly double MinSelectionArea = 20;

        private RegionSelectorResult selectionCandidate;
        private readonly TaskWindowSeeker windowSeeker;

        public RegionSelectorViewModel(
            IFactory<TaskWindowSeeker> taskWindowSeekerFactory,
            [NotNull] ISelectionAdornerViewModel selectionAdorner,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler)
        {
            SelectionAdorner = selectionAdorner.AddTo(Anchors);
            windowSeeker = taskWindowSeekerFactory.Create();
            windowSeeker.SkipNotVisibleWindows = true;

            var refreshRequest = new Subject<Unit>();

            SelectionAdorner.WhenAnyValue(x => x.MousePosition, x => x.Owner).ToUnit()
                .Merge(refreshRequest)
                .Select(x => new { SelectionAdorner.MousePosition, SelectionAdorner.Owner })
                .Where(x => x.Owner != null)
                .Sample(ThrottlingPeriod, bgScheduler)
                .ObserveOn(uiScheduler)
                .Select(x => x.MousePosition.ToScreen(x.Owner))
                .Select(x => new Rectangle(x.X, x.Y, 1, 1))
                .Select(ToRegionResult)
                .Do(x => Log.Debug($"Selection candidate: {x}"))
                .SubscribeSafe(x => SelectionCandidate = x, Log.HandleUiException)
                .AddTo(Anchors);
            
            refreshRequest
                .SubscribeSafe(() => windowSeeker.Refresh(), Log.HandleUiException)
                .AddTo(Anchors);

            Observable.Timer(DateTimeOffset.Now, TimeSpan.FromSeconds(1), bgScheduler).ToUnit()
                .SubscribeSafe(refreshRequest)
                .AddTo(Anchors);
        }

        public ISelectionAdornerViewModel SelectionAdorner { get; }
        
        public IObservable<RegionSelectorResult> SelectWindow(WinSize minSelection)
        {
            return SelectionAdorner.StartSelection()
                .Select(x => new Rect(SelectionAdorner.Owner.PointToScreen(x.Location), x.Size).ToWinRectangle())
                .Do(x => Log.Debug($"Selected region: {x} (min size: {minSelection})"))
                .Select(selection =>
                {
                    if (selection.Width >= minSelection.Width && selection.Height >= minSelection.Height)
                    {
                        return selection;
                    }
                    else
                    {
                        var result = new WinRectangle(selection.X, selection.Y, 0, 0);
                        Log.Debug($"Selected region is less than required({minSelection}, converting selection {selection} to {result}");
                        return result;
                    }
                    
                })
                .Select(ToRegionResult)
                .Do(x => Log.Debug($"Selection Result: {x}"));
        }

        public RegionSelectorResult SelectionCandidate
        {
            get => selectionCandidate;
            private set => this.RaiseAndSetIfChanged(ref selectionCandidate, value);
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