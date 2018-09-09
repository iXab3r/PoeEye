using PoeShared.Modularity;
using Prism.Ioc;
using IModule = Prism.Modularity.IModule;

namespace PoeWhisperMonitor.Prism
{
    using Guards;

    using JetBrains.Annotations;

    using Unity; using Unity.Resolution; using Unity.Attributes;

    public sealed class PoeWhisperMonitorModule : IPoeEyeModule
    {
        private readonly IUnityContainer container;

        public PoeWhisperMonitorModule([NotNull] IUnityContainer container)
        {
            Guard.ArgumentNotNull(container, nameof(container));

            this.container = container;
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            container.AddExtension(new PoeWhisperRegistrations());
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
        }
    }
}
