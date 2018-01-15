using Guards;
using Microsoft.Practices.Unity;
using PoeShared.Modularity;

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
        
        public void Initialize()
        {
            container.AddExtension(new ItemParserRegistrations());
        }
    }
}
