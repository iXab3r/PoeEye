using Guards;
using PoeShared.Modularity;
using Prism.Ioc;
using Unity;

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