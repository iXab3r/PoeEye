using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using Guards;
using JetBrains.Annotations;
using PoeEye.StashGrid.ViewModels;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using Unity.Attributes;

namespace PoeEye.StashGrid.Prism
{
    internal sealed class StashGridBootstrapper : DisposableReactiveObject
    {
        private readonly SerialDisposable activeAnchors = new SerialDisposable();
        private readonly IOverlayWindowController overlayController;
        private readonly IFactory<PoeStashGridViewModel, IOverlayWindowController> viewModelFactory;

        public StashGridBootstrapper(
            [NotNull] [Dependency(WellKnownOverlays.PathOfExileOverlay)]
            IOverlayWindowController overlayController,
            [NotNull] IFactory<PoeStashGridViewModel, IOverlayWindowController> viewModelFactory,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(overlayController, nameof(overlayController));
            Guard.ArgumentNotNull(viewModelFactory, nameof(viewModelFactory));

            this.overlayController = overlayController;
            this.viewModelFactory = viewModelFactory;

            HandleAvailability(true);
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