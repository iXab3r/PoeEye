using Guards;
using Microsoft.Practices.Unity;
using IModule = Prism.Modularity.IModule;

namespace PoeEye.StashRealtimeApi.Prism
{
    public sealed class StashRealtimeModule : IModule
    {
        private readonly IUnityContainer container;

        public StashRealtimeModule(IUnityContainer container)
        {
            Guard.ArgumentNotNull(container, nameof(container));

            this.container = container;
        }

        public void Initialize()
        {
            container.AddExtension(new LiveRegistrations());
        }
    }
}
