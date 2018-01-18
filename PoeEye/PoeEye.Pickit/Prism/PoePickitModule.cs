using System.Reactive.Disposables;
using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeShared.Modularity;

namespace PoeEye.Pickit.Prism
{
    public sealed class PoePickitModule : IPoeEyeModule
    {
        private readonly IUnityContainer container;
        private readonly CompositeDisposable anchors = new CompositeDisposable();

        public PoePickitModule([NotNull] IUnityContainer container)
        {
            Guard.ArgumentNotNull(container, nameof(container));

            this.container = container;
        }

        public void Initialize()
        {
            container.AddExtension(new PoePickitRegistrations());
        }
    }
}
