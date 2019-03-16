using System.Reactive.Disposables;
using Guards;
using JetBrains.Annotations;
using PoeBud.Config;
using PoeBud.ViewModels;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using Prism.Ioc;
using Unity;

namespace PoeBud.Prism
{
    public sealed class PoeBudModule : IPoeEyeModule
    {
        private readonly CompositeDisposable anchors = new CompositeDisposable();
        private readonly IUnityContainer container;

        public PoeBudModule([NotNull] IUnityContainer container)
        {
            Guard.ArgumentNotNull(container, nameof(container));

            this.container = container;
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            container.AddExtension(new PoeBudModuleRegistrations());
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            var registrator = container.Resolve<IPoeEyeModulesRegistrator>();
            registrator.RegisterSettingsEditor<PoeBudConfig, PoeBudSettingsViewModel>();

            container.Resolve<PoeBudBootstrapper>().AddTo(anchors);
        }
    }
}