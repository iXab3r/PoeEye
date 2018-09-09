using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Guards;
using JetBrains.Annotations;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.PoeControlPanel.Modularity;
using PoeShared.PoeControlPanel.ViewModels;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using Unity.Attributes;

namespace PoeShared.PoeControlPanel.Prism
{
    internal sealed class ControlPanelBootstrapper : DisposableReactiveObject
    {
        private readonly IOverlayWindowController overlayController;
        private readonly IFactory<PoeControlPanelViewModel, IOverlayWindowController> viewModelFactory;

        private readonly SerialDisposable activeAnchors = new SerialDisposable();

        public ControlPanelBootstrapper(
                [NotNull] IConfigProvider<PoeControlPanelConfig> configProvider,
                [NotNull] [Dependency(WellKnownOverlays.PathOfExileOverlay)] IOverlayWindowController overlayController,
                [NotNull] IFactory<PoeControlPanelViewModel, IOverlayWindowController> viewModelFactory,
                [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(configProvider, nameof(configProvider));
            Guard.ArgumentNotNull(overlayController, nameof(overlayController));
            Guard.ArgumentNotNull(viewModelFactory, nameof(viewModelFactory));

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
