using System.Reactive.Disposables;
using Guards;
using JetBrains.Annotations;
using Unity; using Unity.Resolution; using Unity.Attributes;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using Prism.Ioc;

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

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            container.AddExtension(new PoePoeStashGridRegistrations());
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            container.Resolve<StashGridBootstrapper>().AddTo(anchors);
        }
    }
}
