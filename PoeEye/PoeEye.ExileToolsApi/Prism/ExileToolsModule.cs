using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using Prism.Modularity;

namespace PoeEye.ExileToolsApi.Prism
{
    public sealed class ExileToolsModule : IModule
    {
        private readonly IUnityContainer container;

        public ExileToolsModule([NotNull] IUnityContainer container)
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