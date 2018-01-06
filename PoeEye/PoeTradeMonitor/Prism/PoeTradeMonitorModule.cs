using System.Reactive.Disposables;
using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeEye.TradeMonitor.Modularity;
using PoeEye.TradeMonitor.Services;
using PoeEye.TradeMonitor.ViewModels;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;

namespace PoeEye.TradeMonitor.Prism
{
    public sealed class PoeTradeMonitorModule : IPoeEyeModule
    {
        private readonly IUnityContainer container;
        private readonly CompositeDisposable anchors = new CompositeDisposable();

        public PoeTradeMonitorModule([NotNull] IUnityContainer container)
        {
            Guard.ArgumentNotNull(container, nameof(container));

            this.container = container;
            container.AddExtension(new PoeTradeMonitorModuleRegistrations());
        }

        public void Initialize()
        {
            var registrator = container.Resolve<IPoeEyeModulesRegistrator>();
            registrator.RegisterSettingsEditor<PoeTradeMonitorConfig, PoeTradeMonitorSettingsViewModel>();

            container.Resolve<TradeMonitorBootstrapper>().AddTo(anchors);
            container.Resolve<ControlPanelBootstrapper>().AddTo(anchors);
        }
    }
}
