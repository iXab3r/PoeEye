using Guards;
using JetBrains.Annotations;
using PoeShared.Modularity;
using Prism.Ioc;
using Unity;

namespace PoeShared.Chromium.Prism
{
    public sealed class PoeChromiumModule : IPoeEyeModule
    {
        private readonly IUnityContainer container;

        public PoeChromiumModule([NotNull] IUnityContainer container)
        {
            Guard.ArgumentNotNull(container, nameof(container));

            this.container = container;
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            container.AddExtension(new ChromiumRegistrations());
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
        }
    }
}