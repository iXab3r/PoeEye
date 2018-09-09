using System.Reactive.Disposables;
using Guards;
using JetBrains.Annotations;
using PoeShared.Modularity;
using Prism.Ioc;
using Unity;

namespace PoeEye.Pickit.Prism
{
    public sealed class PoePickitModule : IPoeEyeModule
    {
        private readonly CompositeDisposable anchors = new CompositeDisposable();
        private readonly IUnityContainer container;

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