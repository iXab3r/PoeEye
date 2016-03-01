using ConfigurationModuleCatalog = Prism.Modularity.ConfigurationModuleCatalog;
using IModuleCatalog = Prism.Modularity.IModuleCatalog;
using UnityBootstrapper = Prism.Unity.UnityBootstrapper;

namespace PoeEyeUi.Prism
{
    using System.Windows;

    using Microsoft.Practices.Unity;

    using PoeShared;
    using PoeShared.Prism;

    using PoeTrade.ViewModels;
    using PoeTrade.Views;

    using PoeWhisperMonitor.Prism;

    internal sealed class PoeEyeBootstrapper : UnityBootstrapper
    {
        protected override DependencyObject CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void InitializeShell()
        {
            RegisterExtensions();

            var window = (Window)Shell;
            Application.Current.MainWindow = window;
            window.Show();
        }

        protected override IModuleCatalog CreateModuleCatalog()
        {
            return new ConfigurationModuleCatalog();
        }

        public override void Run(bool runWithDefaultConfiguration)
        {
            base.Run(runWithDefaultConfiguration);

            var window = (Window)Shell;
            var viewModel = Container.Resolve<IMainWindowViewModel>();

            window.DataContext = viewModel;
        }

        private void RegisterExtensions()
        {
            Log.Instance.Debug("Initializing DI container...");
            Container.AddExtension(new CommonRegistrations());
            Container.AddExtension(new UiRegistrations());
        }
    }
}