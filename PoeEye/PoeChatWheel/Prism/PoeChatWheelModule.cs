using System.Windows;
using Guards;
using JetBrains.Annotations;
using Unity; using Unity.Resolution; using Unity.Attributes;
using PoeChatWheel.Modularity;
using PoeChatWheel.ViewModels;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Prism;
using Prism.Ioc;

namespace PoeChatWheel.Prism
{
    public sealed class PoeChatWheelModule : IPoeEyeModule
    {
        private readonly IUnityContainer container;

        public PoeChatWheelModule([NotNull] IUnityContainer container)
        {
            Guard.ArgumentNotNull(container, nameof(container));

            this.container = container;
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            container.AddExtension(new PoeChatWheelRegistrations());
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            var registrator = container.Resolve<IPoeEyeModulesRegistrator>();
            registrator.RegisterSettingsEditor<PoeChatWheelConfig, PoeChatWheelSettingsViewModel>();

            var overlayController = container.Resolve<IOverlayWindowController>(WellKnownOverlays.PathOfExileOverlay);
            var overlayModel = container.Resolve<IPoeChatWheelViewModel>(new DependencyOverride(typeof(IOverlayWindowController), overlayController));
            overlayController.RegisterChild(overlayModel);
        }
    }
}
