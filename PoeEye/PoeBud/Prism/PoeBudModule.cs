using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeBud.Config;
using PoeBud.ViewModels;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Prism;

namespace PoeBud.Prism
{
    public sealed class PoeBudModule : IPoeEyeModule
    {
        private readonly IUnityContainer container;

        public PoeBudModule([NotNull] IUnityContainer container)
        {
            Guard.ArgumentNotNull(container, nameof(container));

            this.container = container;
        }

        public void Initialize()
        {
            container.AddExtension(new PoeBudModuleRegistrations());

            var registrator = container.Resolve<IPoeEyeModulesRegistrator>();
            registrator.RegisterSettingsEditor<PoeBudConfig, PoeBudSettingsViewModel>();

            var overlayController = container.Resolve<IOverlayWindowController>(WellKnownOverlays.PathOfExileOverlay);
            var overlayModel = container.Resolve<PoeBudViewModel>(new DependencyOverride(typeof(IOverlayWindowController), overlayController));
            overlayController.RegisterChild(overlayModel);
        }
    }
}
