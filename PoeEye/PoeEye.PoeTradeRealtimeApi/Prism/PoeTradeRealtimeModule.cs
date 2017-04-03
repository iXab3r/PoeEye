using Guards;
using Microsoft.Practices.Unity;
using Prism.Modularity;

namespace PoeEye.PoeTradeRealtimeApi.Prism
{
    public sealed class PoeTradeRealtimeModule : IModule
    {
        private readonly IUnityContainer container;

        public PoeTradeRealtimeModule(IUnityContainer container)
        {
            this.container = container;
            Guard.ArgumentNotNull(container, nameof(container));
        }

        public void Initialize()
        {
            container.AddExtension(new LiveRegistrations());
        }
    }
}
