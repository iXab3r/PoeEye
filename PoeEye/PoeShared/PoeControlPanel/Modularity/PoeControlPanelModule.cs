using System.Reactive.Disposables;
using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeShared.Modularity;
using PoeShared.PoeControlPanel.Prism;
using PoeShared.Prism;
using PoeShared.Scaffolding;

namespace PoeShared.PoeControlPanel.Modularity
{
    public sealed class PoeControlPanelModule : IPoeEyeModule
    {
        private readonly IUnityContainer container;
        private readonly CompositeDisposable anchors = new CompositeDisposable();

        public PoeControlPanelModule([NotNull] IUnityContainer container)
        {
            Guard.ArgumentNotNull(container, nameof(container));

            this.container = container;
            container.AddExtension(new CommonRegistrations());
        }

        public void Initialize()
        {
            container.Resolve<ControlPanelBootstrapper>().AddTo(anchors);
        }
    }
}