using System;
using System.Drawing;
using System.Reactive.Concurrency;
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
    private readonly IWindowRegionSelector windowRegionSelector;

    public ScreenRegionSelectorService(
        [Dependency(WellKnownWindows.MainWindow)] IWindowViewController viewController,
        [Dependency(WellKnownWindows.AllWindows)] IOverlayWindowController overlayController,
        [Dependency(WellKnownSchedulers.UIIdle)] IScheduler uiScheduler,
        IFactory<IWindowRegionSelector> regionSelectorWindowFactory)
    {
        Log.Debug(() => $"Initializing region selector service");
        this.viewController = viewController;
        this.overlayController = overlayController;
        windowRegionSelector = regionSelectorWindowFactory.Create();
        windowRegionSelector.IsVisible = false;
        uiScheduler.Schedule(() =>
        {
            Log.Debug(() => $"Registering region selector overlay");
            overlayController.RegisterChild(windowRegionSelector).AddTo(Anchors);
        });
    }

    public async Task<RegionSelectorResult> SelectRegion(Size minSelection)
    {
        var workingArea = SystemInformation.VirtualScreen;
        windowRegionSelector.NativeBounds = workingArea;
        using var regionSelectorAnchors = windowRegionSelector.Show();

        Log.Debug(() => $"Showing new selector window: {windowRegionSelector}");
        viewController.Minimize();
        try
        {
            Log.Debug(() => $"Awaiting for selection result from {windowRegionSelector}");
            return await windowRegionSelector.StartSelection(minSelection);
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