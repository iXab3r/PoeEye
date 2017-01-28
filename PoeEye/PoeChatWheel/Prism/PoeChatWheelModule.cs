using System.Windows;
using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeChatWheel.Modularity;
using PoeChatWheel.ViewModels;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Prism;

namespace PoeChatWheel.Prism
{
    public sealed class PoeChatWheelModule : IPoeEyeModule
    {
        private readonly IUnityContainer container;

        public PoeChatWheelModule([NotNull] IUnityContainer container)
        {
            Guard.ArgumentNotNull(() => container);

            this.container = container;
        }

        public void Initialize()
        {
            container.AddExtension(new PoeChatWheelRegistrations());

            var registrator = container.Resolve<IPoeEyeModulesRegistrator>();
            registrator.RegisterSettingsEditor<PoeChatWheelConfig, PoeChatWheelSettingsViewModel>();

            var overlayController = container.Resolve<IOverlayWindowController>(WellKnownOverlays.PathOfExileLayeredOverlay);
            var overlayModel = container.Resolve<IPoeChatWheelViewModel>(new DependencyOverride(typeof(IOverlayWindowController), overlayController));
            overlayController.RegisterChild(overlayModel);
        }
    }
}