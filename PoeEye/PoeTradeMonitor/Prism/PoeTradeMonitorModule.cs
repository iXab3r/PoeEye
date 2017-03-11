using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeEye.TradeMonitor.Modularity;
using PoeEye.TradeMonitor.ViewModels;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Prism;

namespace PoeEye.TradeMonitor.Prism
{
    public sealed class PoeTradeMonitorModule : IPoeEyeModule
    {
        private readonly IUnityContainer container;

        public PoeTradeMonitorModule([NotNull] IUnityContainer container)
        {
            Guard.ArgumentNotNull(() => container);

            this.container = container;
        }

        public void Initialize()
        {
            container.AddExtension(new PoeTradeMonitorModuleRegistrations());

            var registrator = container.Resolve<IPoeEyeModulesRegistrator>();
            registrator.RegisterSettingsEditor<PoeTradeMonitorConfig, PoeTradeMonitorSettingsViewModel>();

            var overlayController = container.Resolve<IOverlayWindowController>(WellKnownOverlays.PathOfExileLayeredOverlay);
            var overlayModel = container.Resolve<PoeTradeMonitorViewModel>(new DependencyOverride(typeof(IOverlayWindowController), overlayController));
            overlayController.RegisterChild(overlayModel);
        }
    }
}