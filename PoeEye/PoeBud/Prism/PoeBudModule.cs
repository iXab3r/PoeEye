using System;
using Guards;
using JetBrains.Annotations;
using Unity; using Unity.Resolution; using Unity.Attributes;
using PoeBud.Config;
using PoeBud.ViewModels;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Prism;
using Prism.Ioc;

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

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            container.AddExtension(new PoeBudModuleRegistrations());
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            var registrator = container.Resolve<IPoeEyeModulesRegistrator>();
            registrator.RegisterSettingsEditor<PoeBudConfig, PoeBudSettingsViewModel>();

            var overlayController = container.Resolve<IOverlayWindowController>(WellKnownOverlays.PathOfExileOverlay);
            var overlayModel = container.Resolve<PoeBudViewModel>(new DependencyOverride(typeof(IOverlayWindowController), overlayController));
            overlayController.RegisterChild(overlayModel);
        }
    }
}
