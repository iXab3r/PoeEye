using Guards;
using PoeShared.Modularity;
using Prism.Ioc;
using Unity;

namespace PoeEye.PathOfExileTrade.Prism
{
    public sealed class PathOfExileTradeModule : IPoeEyeModule
    {
        private readonly IUnityContainer container;

        public PathOfExileTradeModule(IUnityContainer container)
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