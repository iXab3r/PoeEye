using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Windows.Input;
using Newtonsoft.Json;
using PoeChatWheel;
using PoeChatWheel.ViewModels;
using PoeEye.Config;
using PoeEye.Converters;
using PoeEye.PoeTrade.Shell.ViewModels;
using PoeShared.Common;
using PoeShared.Communications.Chromium;
using PoeShared.Modularity;
using PoeShared.PoeTrade;
using PoeShared.PoeTrade.Query;
using PoeShared.Scaffolding;
using Prism.Modularity;
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

    internal sealed class PoeEyeBootstrapper : UnityBootstrapper
    {
        protected override DependencyObject CreateShell()
        {
            return Container.Resolve<PoeTrade.Shell.Views.MainWindow>();
        }
        
        protected override void InitializeShell()
        {
            base.InitializeShell();

            Log.Instance.Info($"[Bootstrapper] Initializing shell...");
            var sw = Stopwatch.StartNew();
            
            Mouse.OverrideCursor = new Cursor(new MemoryStream(Properties.Resources.PathOfExile_102));
            var splashWindow = new SplashScreen("Resources\\Splash.png");
            splashWindow.Show(true, false);
            
            var window = (Window)Shell;
            Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            Application.Current.MainWindow = window;

            Observable
                .FromEventPattern<RoutedEventHandler, RoutedEventArgs>(h => window.Loaded += h, h => window.Loaded -= h)
                .Take(1)
                .Subscribe(
                    () =>
                    {
                        sw.Stop();
                        Log.Instance.Info($"[Bootstrapper] Shell initialization has taken {sw.ElapsedMilliseconds}ms");
                    });
        }

        protected override IModuleCatalog CreateModuleCatalog()
        {
            return new ConfigurationModuleCatalog();
        }
        
        public override void Run(bool runWithDefaultConfiguration)
        {
            base.Run(runWithDefaultConfiguration);
            
            var moduleCatalog = Container.Resolve<IModuleCatalog>();
            var modules = moduleCatalog.Modules.ToArray();
            Log.Instance.Info($"Modules list:\n{modules.Select(x => new { x.ModuleName, x.ModuleType, x.State, x.InitializationMode, x.DependsOn }).DumpToTextRaw()}");

            var window = (Window)Shell;
            
            var viewModel = Container.Resolve<IMainWindowViewModel>();
            window.DataContext = viewModel;
            window.Show();
        }

        public void Dispose()
        {
            Log.Instance.Info($"Disposing Chromium...");
            var chromium = Container.TryResolve<IChromiumBootstrapper>();
            chromium?.Dispose();
        }
    }
}