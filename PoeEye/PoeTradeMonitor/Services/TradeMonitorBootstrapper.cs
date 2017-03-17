using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeEye.TradeMonitor.Modularity;
using PoeEye.TradeMonitor.ViewModels;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeEye.TradeMonitor.Services
{
    internal sealed class TradeMonitorBootstrapper : DisposableReactiveObject
    {
        private readonly IOverlayWindowController overlayController;
        private readonly IFactory<PoeTradeMonitorViewModel, IOverlayWindowController> viewModelFactory;

        private readonly SerialDisposable activeAnchors = new SerialDisposable();

        public TradeMonitorBootstrapper(
                IConfigProvider<PoeTradeMonitorConfig> configProvider,
                [Dependency(WellKnownOverlays.PathOfExileLayeredOverlay)] IOverlayWindowController overlayController,
                IFactory<PoeTradeMonitorViewModel, IOverlayWindowController> viewModelFactory,
                [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(() => configProvider);
            Guard.ArgumentNotNull(() => overlayController);
            Guard.ArgumentNotNull(() => viewModelFactory);

            this.overlayController = overlayController;
            this.viewModelFactory = viewModelFactory;

            configProvider
                .ListenTo(x => x.IsEnabled)
                .ObserveOn(uiScheduler)
                .Subscribe(HandleAvailability)
                .AddTo(Anchors);
        }

        private void HandleAvailability(bool isEnabled)
        {
            var anchors = new CompositeDisposable();
            activeAnchors.Disposable = anchors;

            if (!isEnabled)
            {
                return;
            }

            var viewModel = viewModelFactory.Create(overlayController);
            viewModel.AddTo(anchors);

            overlayController.RegisterChild(viewModel).AddTo(anchors);
        }
    }
}