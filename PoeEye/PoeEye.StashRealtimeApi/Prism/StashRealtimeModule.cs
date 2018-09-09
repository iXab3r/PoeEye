using Guards;
using PoeShared.Modularity;
using Prism.Ioc;
using Unity; using Unity.Resolution; using Unity.Attributes;
using IModule = Prism.Modularity.IModule;

namespace PoeEye.StashRealtimeApi.Prism
{
    public sealed class StashRealtimeModule : IPoeEyeModule
    {
        private readonly IUnityContainer container;

        public StashRealtimeModule(IUnityContainer container)
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
