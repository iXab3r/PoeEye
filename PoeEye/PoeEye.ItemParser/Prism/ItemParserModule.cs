using Guards;
using PoeShared.Modularity;
using Prism.Ioc;
using Unity;

namespace PoeEye.ItemParser.Prism
{
    internal sealed class ItemParserModule : IPoeEyeModule
    {
        private readonly IUnityContainer container;

        public ItemParserModule(IUnityContainer container)
        {
            Guard.ArgumentNotNull(container, nameof(container));

            this.container = container;
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            container.AddExtension(new ItemParserRegistrations());
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
        }

        public void Initialize()
        {
        }
    }
}