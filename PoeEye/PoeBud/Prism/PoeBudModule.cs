using System.Windows.Data;
using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeBud.Config;
using PoeBud.ViewModels;
using PoeBud.Views;
using PoeShared.Modularity;

namespace PoeBud.Prism
{
    public sealed class PoeBudModule : IPoeEyeModule
    {
        private readonly IUnityContainer container;

        public PoeBudModule([NotNull] IUnityContainer container)
        {
            Guard.ArgumentNotNull(() => container);

            this.container = container;
        }

        public void Initialize()
        {
            container.AddExtension(new PoeBudModuleRegistrations());

            var registrator = container.Resolve<IPoeEyeModulesRegistrator>();
            registrator.RegisterSettingsEditor<PoeBudConfig, PoeBudSettingsViewModel>();

            var viewModel = container.Resolve<OverlayWindowViewModel>();
            var overlay = new OverlayWindowView { DataContext = viewModel };
            overlay.Show();
        }
    }
}