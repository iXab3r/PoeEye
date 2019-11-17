
using JetBrains.Annotations;
using Prism.Ioc;
using Prism.Modularity;
using Unity;

namespace PoeShared.Prism
{
    public sealed class PoeSharedWpfModule : IModule
    {
        private readonly IUnityContainer container;

        public PoeSharedWpfModule([NotNull] IUnityContainer container)
        {
            Guard.ArgumentNotNull(container, nameof(container));

            this.container = container;
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            container.AddExtension(new WpfCommonRegistrations());
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
        }
    }
}