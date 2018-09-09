using System.Reactive.Disposables;
using Guards;
using JetBrains.Annotations;
using PoeEye.TradeMonitor.Modularity;
using PoeEye.TradeMonitor.ViewModels;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using Prism.Ioc;
using Unity;

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
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            container.AddExtension(new PoeTradeMonitorModuleRegistrations());
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            var registrator = container.Resolve<IPoeEyeModulesRegistrator>();
            registrator.RegisterSettingsEditor<PoeTradeMonitorConfig, PoeTradeMonitorSettingsViewModel>();

            container.Resolve<TradeMonitorBootstrapper>().AddTo(anchors);
        }
    }
}
