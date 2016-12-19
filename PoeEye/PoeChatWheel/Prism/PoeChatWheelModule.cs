using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using IModule = Prism.Modularity.IModule;

namespace PoeChatWheel.Prism
{
    public sealed class PoeChatWheelModule : IModule
    {
        private readonly IUnityContainer container;

        public PoeChatWheelModule([NotNull] IUnityContainer container)
        {
            Guard.ArgumentNotNull(() => container);

            this.container = container;
        }

        public void Initialize()
        {
            container.AddExtension(new PoeChatWheelRegistrations());
        }
    }
}