using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeEye.TradeSummary.ViewModels;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Prism;

namespace PoeEye.TradeSummary.Prism
{
    public sealed class PoeTradeSummaryModule : IPoeEyeModule
    {
        private readonly IUnityContainer container;

        public PoeTradeSummaryModule([NotNull] IUnityContainer container)
        {
            Guard.ArgumentNotNull(container, nameof(container));

            this.container = container;
        }

        public void Initialize()
        {
            container.AddExtension(new PoeTradeSummaryModuleRegistrations());

            var overlayController = container.Resolve<IOverlayWindowController>(WellKnownOverlays.PathOfExileOverlay);
            var overlayModel = container.Resolve<PoeTradeSummaryViewModel>(new DependencyOverride(typeof(IOverlayWindowController), overlayController));
            overlayController.RegisterChild(overlayModel);
        }
    }
}
