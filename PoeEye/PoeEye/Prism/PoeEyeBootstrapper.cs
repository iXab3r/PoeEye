using System;
using System.Reactive.Linq;
using System.Windows.Input;
using PoeChatWheel;
using PoeChatWheel.ViewModels;
using PoeEye.Config;
using ReactiveUI;
using ConfigurationModuleCatalog = Prism.Modularity.ConfigurationModuleCatalog;
using IModuleCatalog = Prism.Modularity.IModuleCatalog;
using UnityBootstrapper = Prism.Unity.UnityBootstrapper;

namespace PoeEye.Prism
{
    using System.Windows;

    using Microsoft.Practices.Unity;

    using PoeShared;
    using PoeShared.Prism;

    using PoeTrade.ViewModels;
    using PoeTrade.Views;

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

            InitializeChatWheel();
        }

        private void InitializeChatWheel()
        {
            var chatWheel = Container.Resolve<IPoeChatWheelViewModel>();

            var settings = Container.Resolve<IPoeEyeConfigProvider>();
            settings.WhenAnyValue(x => x.ActualConfig)
                .Select(hotkey => new KeyGestureConverter().ConvertFromInvariantString(hotkey.ChatWheelHotkey) as KeyGesture)
                .Subscribe(hotkey => chatWheel.Hotkey = hotkey);

            var window = new ChatWheelWindow(chatWheel);
            window.Show();
        }

        private void RegisterExtensions()
        {
            Log.Instance.Debug("Initializing DI container...");
            Container.AddExtension(new CommonRegistrations());
            Container.AddExtension(new UiRegistrations());
        }
    }
}