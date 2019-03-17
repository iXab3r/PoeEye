using Guards;
using JetBrains.Annotations;
using PoeEye.TradeSummaryOverlay.Modularity;
using PoeEye.TradeSummaryOverlay.ViewModels;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using Prism.Ioc;
using Unity;
using Unity.Resolution;

namespace PoeEye.TradeSummaryOverlay.Prism
{
    [UsedImplicitly]
    public sealed class PoeTradeSummaryModule : DisposableReactiveObject, IPoeEyeModule
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
            var registrator = container.Resolve<IPoeEyeModulesRegistrator>();
            registrator.RegisterSettingsEditor<PoeTradeSummaryConfig, PoeTradeSummarySettingsViewModel>();
            
            container.Resolve<PoeTradeSummaryBootstrapper>().AddTo(Anchors);
        }
    }
}