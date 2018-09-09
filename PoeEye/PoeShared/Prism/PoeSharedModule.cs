using Guards;
using JetBrains.Annotations;
using Unity; using Unity.Resolution; using Unity.Attributes;
using PoeShared.Modularity;
using Prism.Ioc;
using Unity;

namespace PoeShared.Prism
{
    public sealed class PoeSharedModule : IPoeEyeModule
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
        }
    }
}