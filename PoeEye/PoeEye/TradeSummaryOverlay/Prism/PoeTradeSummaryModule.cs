using Guards;
using JetBrains.Annotations;
using Unity; using Unity.Resolution; using Unity.Attributes;
using PoeEye.TradeSummaryOverlay.ViewModels;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Prism;
using Prism.Ioc;

namespace PoeEye.TradeSummaryOverlay.Prism
{
    public sealed class PoeTradeSummaryModule : IPoeEyeModule
    {
        private readonly IUnityContainer container;

        public PoeTradeSummaryModule([NotNull] IUnityContainer container)
        {
            Guard.ArgumentNotNull(container, nameof(container));

            this.container = container;
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            container.AddExtension(new PoeTradeSummaryModuleRegistrations());
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            var overlayController = container.Resolve<IOverlayWindowController>(WellKnownOverlays.PathOfExileOverlay);
            var overlayModel = container.Resolve<PoeTradeSummaryViewModel>(new DependencyOverride(typeof(IOverlayWindowController), overlayController));
            overlayController.RegisterChild(overlayModel);
        }
    }
}
