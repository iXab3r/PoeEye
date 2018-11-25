using Guards;
using JetBrains.Annotations;
using PoeShared.Modularity;
using Prism.Ioc;
using Unity;

namespace PoeShared.Prism
{
    public sealed class PoeSharedPathOfExileModule : IPoeEyeModule
    {
        private readonly IUnityContainer container;

        public PoeSharedPathOfExileModule([NotNull] IUnityContainer container)
        {
            Guard.ArgumentNotNull(container, nameof(container));

            this.container = container;
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            container.AddExtension(new CommonPathOfExileRegistrations());
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
        }
    }
}