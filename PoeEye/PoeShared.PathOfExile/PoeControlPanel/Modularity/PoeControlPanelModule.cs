using System.Reactive.Disposables;
using Guards;
using JetBrains.Annotations;
using PoeShared.Modularity;
using PoeShared.PoeControlPanel.Prism;
using PoeShared.Scaffolding;
using Prism.Ioc;
using Unity;

namespace PoeShared.PoeControlPanel.Modularity
{
    public sealed class PoeControlPanelModule : IPoeEyeModule
    {
        private readonly CompositeDisposable anchors = new CompositeDisposable();
        private readonly IUnityContainer container;

        public PoeControlPanelModule([NotNull] IUnityContainer container)
        {
            Guard.ArgumentNotNull(container, nameof(container));

            this.container = container;
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            container.Resolve<ControlPanelBootstrapper>().AddTo(anchors);
        }
    }
}