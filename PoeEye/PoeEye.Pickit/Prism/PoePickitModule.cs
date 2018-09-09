using System.Reactive.Disposables;
using Guards;
using JetBrains.Annotations;
using Unity; using Unity.Resolution; using Unity.Attributes;
using PoeShared.Modularity;
using Prism.Ioc;

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

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            container.AddExtension(new PoePickitRegistrations());
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
        }
    }
}
