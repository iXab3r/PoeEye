using System;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Windows.Input;
using Newtonsoft.Json;
using PoeChatWheel;
using PoeChatWheel.ViewModels;
using PoeEye.Config;
using PoeEye.Converters;
using PoeShared.Common;
using PoeShared.Modularity;
using PoeShared.PoeTrade;
using PoeShared.PoeTrade.Query;
using PoeShared.Scaffolding;
using Prism.Unity;
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
            base.InitializeShell();

            RegisterExtensions();
            InitializeConfigConverters();

            Mouse.OverrideCursor = new Cursor(new MemoryStream(Properties.Resources.PathOfExile_102));

            var splashWindow = new SplashScreen("Resources\\Splash.png");
            splashWindow.Show(true, true);

            var window = (Window)Shell;
            Application.Current.MainWindow = window;
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
            window.Show();
        }

        private void InitializeConfigConverters()
        {
            var configProvider = Container.TryResolve<IConfigProvider>();
            var converters = new JsonConverter[]
            {
                new ConcreteListTypeConverter<IPoeQueryInfo, PoeQueryInfo>(),
                new ConcreteListTypeConverter<IPoeItemType, PoeItemType>(),
                new ConcreteListTypeConverter<IPoeItem, PoeItem>(),
                new ConcreteListTypeConverter<IPoeItemMod, PoeItemMod>(),
                new ConcreteListTypeConverter<IPoeLinksInfo, PoeLinksInfo>(),
                new ConcreteListTypeConverter<IPoeQueryModsGroup, PoeQueryModsGroup>(),
                new ConcreteListTypeConverter<IPoeQueryRangeModArgument, PoeQueryRangeModArgument>()
            };
            converters.ForEach(configProvider.RegisterConverter);
        }

        private void RegisterExtensions()
        {
            Log.Instance.Debug("Initializing DI container...");
            Container.AddExtension(new CommonRegistrations());
            Container.AddExtension(new UiRegistrations());
        }
    }
}