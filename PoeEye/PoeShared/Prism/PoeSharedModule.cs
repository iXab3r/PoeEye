using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeShared.Modularity;

namespace PoeShared.Prism
{
    public sealed class PoeSharedModule : IPoeEyeModule
    {
        private readonly IUnityContainer container;

        public PoeSharedModule([NotNull] IUnityContainer container)
        {
            Guard.ArgumentNotNull(container, nameof(container));

            this.container = container;
            container.AddExtension(new CommonRegistrations());
        }

        public void Initialize()
        {
        }
    }
}