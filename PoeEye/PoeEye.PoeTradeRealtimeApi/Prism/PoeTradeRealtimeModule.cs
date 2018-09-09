using Guards;
using PoeShared.Modularity;
using Prism.Ioc;
using Unity; using Unity.Resolution; using Unity.Attributes;
using Prism.Modularity;
using Unity;

namespace PoeEye.PoeTradeRealtimeApi.Prism
{
    public sealed class PoeTradeRealtimeModule : IPoeEyeModule
    {
        private readonly IUnityContainer container;

        public PoeTradeRealtimeModule(IUnityContainer container)
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
