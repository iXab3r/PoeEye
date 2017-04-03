using IModule = Prism.Modularity.IModule;

namespace PoeEye.PoeTrade.Prism
{
    using Guards;

    using Microsoft.Practices.Unity;

    public sealed class PoeTradeModule : IModule
    {
        private readonly IUnityContainer container;

        public PoeTradeModule(IUnityContainer container)
        {
            this.container = container;
            Guard.ArgumentNotNull(container, nameof(container));
        }

        public void Initialize()
        {
            container.AddExtension(new LiveRegistrations());
        }
    }
}
