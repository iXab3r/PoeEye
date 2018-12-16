using Guards;
using PoeShared.Modularity;
using Prism.Ioc;
using Unity;

namespace PoeEye.PoeTrade.Prism
{
    public sealed class PoeTradeModule : IPoeEyeModule
    {
        private readonly IUnityContainer container;

        public PoeTradeModule(IUnityContainer container)
        {
            Guard.ArgumentNotNull(container, nameof(container));
            this.container = container;
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