
using JetBrains.Annotations;
using PoeShared.Modularity;
using Prism.Ioc;
using Prism.Modularity;
using Unity;

namespace PoeShared.Prism
{
    public sealed class PoeSharedModule : IModule
    {
        private readonly IUnityContainer container;

        public PoeSharedModule([NotNull] IUnityContainer container)
        {
            Guard.ArgumentNotNull(container, nameof(container));

            this.container = container;
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            container.AddExtension(new CommonRegistrations());
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            var configSerializer = container.Resolve<IConfigSerializer>();
            configSerializer.RegisterConverter(new PoeConfigConverter());
        }
    }
}