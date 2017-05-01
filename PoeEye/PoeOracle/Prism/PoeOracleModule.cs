using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeOracle.ViewModels;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;

namespace PoeOracle.Prism
{
    public sealed class PoeOracleModule : IPoeEyeModule
    {
        private readonly IUnityContainer container;

        public PoeOracleModule([NotNull] IUnityContainer container)
        {
            Guard.ArgumentNotNull(container, nameof(container));

            this.container = container;
        }

        public void Initialize()
        {
            container.AddExtension(new PoeOracleModuleRegistrations());

            var overlayController = container.Resolve<IOverlayWindowController>(WellKnownOverlays.PathOfExileOverlay);
            var overlayModel = container.Resolve<PoeOracleViewModel>(new DependencyOverride(typeof(IOverlayWindowController), overlayController));
            overlayController.RegisterChild(overlayModel);
        }
    }
}
