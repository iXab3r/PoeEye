using System.Reactive.Disposables;
using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeShared.Modularity;
using PoeShared.Scaffolding;

namespace PoeEye.StashGrid.Prism
{
    public sealed class PoeStashGridModule : IPoeEyeModule
    {
        private readonly IUnityContainer container;
        private readonly CompositeDisposable anchors = new CompositeDisposable();

        public PoeStashGridModule([NotNull] IUnityContainer container)
        {
            Guard.ArgumentNotNull(container, nameof(container));

            this.container = container;
        }

        public void Initialize()
        {
            container.AddExtension(new PoePoeStashGridRegistrations());
            
            container.Resolve<StashGridBootstrapper>().AddTo(anchors);
        }
    }
}
