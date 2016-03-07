using IModule = Prism.Modularity.IModule;

namespace PoeWhisperMonitor.Prism
{
    using Guards;

    using JetBrains.Annotations;

    using Microsoft.Practices.Unity;

    public sealed class PoeWhisperMonitorModule : IModule
    {
        private readonly IUnityContainer container;

        public PoeWhisperMonitorModule([NotNull] IUnityContainer container)
        {
            Guard.ArgumentNotNull(() => container);

            this.container = container;
        }

        public void Initialize()
        {
            container.AddExtension(new PoeWhisperRegistrations());
        }
    }
}