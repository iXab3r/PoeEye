using PoeShared.Modularity;
using Prism.Ioc;
using Unity;
using IModule = Prism.Modularity.IModule;

namespace PoeEye.PoeTrade.Prism
{
    using Guards;

    using Unity; using Unity.Resolution; using Unity.Attributes;

    public sealed class PoeTradeModule : IPoeEyeModule
    {
        private readonly IUnityContainer container;

        public PoeTradeModule(IUnityContainer container)
        {
            this.container = container;
            Guard.ArgumentNotNull(container, nameof(container));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            container.AddExtension(new LiveRegistrations());
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
        }
    }
}
