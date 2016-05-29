using Guards;
using Microsoft.Practices.Unity;
using Prism.Modularity;

namespace PoeEye.ExileToolsApi.Prism
{
    public sealed class ExileToolsModule : IModule
    {
        private readonly IUnityContainer container;

        public ExileToolsModule(IUnityContainer container)
        {
            this.container = container;
            Guard.ArgumentNotNull(() => container);
        }

        public void Initialize()
        {
            container.AddExtension(new LiveRegistrations());
        }
    }
}