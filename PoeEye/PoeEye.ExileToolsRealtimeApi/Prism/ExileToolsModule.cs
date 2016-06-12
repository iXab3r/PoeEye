using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeEye.ExileToolsApi.Prism;
using Prism.Modularity;

namespace PoeEye.ExileToolsRealtimeApi.Prism
{
    public sealed class ExileToolsRealtimeModule : IModule
    {
        private readonly IUnityContainer container;

        public ExileToolsRealtimeModule([NotNull] IUnityContainer container)
        {
            Guard.ArgumentNotNull(() => container);
            this.container = container;
        }

        public void Initialize()
        {
            container.AddExtension(new LiveRegistrations());
        }
    }
}