using System;
using System.Drawing;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using PoeShared.Logging;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.RegionSelector.ViewModels;
using PoeShared.Scaffolding;
using Unity;

namespace PoeShared.RegionSelector.Services;

internal sealed class ScreenRegionSelectorService : DisposableReactiveObject, IScreenRegionSelectorService
{
    private static readonly IFluentLog Log = typeof(ScreenRegionSelectorService).PrepareLogger();

    private readonly IWindowViewController viewController;
    private readonly IOverlayWindowController overlayController;
    private readonly IRegionSelectorViewModel regionSelector;

    public ScreenRegionSelectorService(
        [Dependency(WellKnownWindows.MainWindow)] IWindowViewController viewController,
        [Dependency(WellKnownWindows.AllWindows)] IOverlayWindowController overlayController,
        [Dependency(WellKnownSchedulers.UIIdle)] IScheduler uiScheduler,
        IFactory<IRegionSelectorViewModel> regionSelectorWindowFactory)
    {
        Log.Debug(() => $"Initializing region selector service");
        this.viewController = viewController;
        this.overlayController = overlayController;
        regionSelector = regionSelectorWindowFactory.Create();
        regionSelector.IsVisible = false;
        uiScheduler.Schedule(() =>
        {
            Log.Debug(() => $"Registering region selector overlay");
            overlayController.RegisterChild(regionSelector).AddTo(Anchors);
        });
    }

    public async Task<RegionSelectorResult> SelectRegion(Size minSelection)
    {
        var workingArea = SystemInformation.VirtualScreen;
        regionSelector.NativeBounds = workingArea;
        using var regionSelectorAnchors = regionSelector.Show();

        Log.Debug(() => $"Showing new selector window: {regionSelector}");
        viewController.Minimize();
        try
        {
            Log.Debug(() => $"Awaiting for selection result from {regionSelector}");
            return await regionSelector.StartSelection(minSelection);
        }
        catch (Exception ex)
        {
            Log.Warn("Failed to do region selection", ex);
            return new RegionSelectorResult();
        }
        finally
        {
            viewController.Show();
        }
    }
}