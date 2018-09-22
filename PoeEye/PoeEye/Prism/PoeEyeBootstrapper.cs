using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Common.Logging;
using PoeEye.PoeTrade.Shell.Services;
using PoeEye.PoeTrade.Shell.ViewModels;
using PoeEye.PoeTrade.Shell.Views;
using PoeEye.Properties;
using PoeShared.Communications.Chromium;
using PoeShared.Native;
using PoeShared.Scaffolding;
using Prism.Modularity;
using Prism.Unity;
using Unity;

namespace PoeEye.Prism
{
    internal sealed class PoeEyeBootstrapper : UnityBootstrapper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PoeEyeBootstrapper));

        protected override DependencyObject CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void InitializeShell()
        {
            base.InitializeShell();

            Log.Info("Initializing shell...");
            var sw = Stopwatch.StartNew();

            Mouse.OverrideCursor = new Cursor(new MemoryStream(Resources.PathOfExile_102));
            

            var window = (Window)Shell;

            var splashWindow = new SplashWindow(window);
            
            Observable
                .FromEventPattern<EventHandler, EventArgs>(h => window.ContentRendered += h, h => window.ContentRendered -= h)
                .Take(1)
                .Subscribe(
                    () =>
                    {
                        Log.Debug($"Window rendered");
                        Application.Current.MainWindow = window;
                        splashWindow.Close();
                        Log.Info($"Window+Shell initialization has taken {sw.ElapsedMilliseconds}ms");
                    });
            
            Observable
                .FromEventPattern<RoutedEventHandler, RoutedEventArgs>(h => window.Loaded += h, h => window.Loaded -= h)
                .Take(1)
                .Subscribe(
                    () =>
                    {
                        Log.Debug($"Window loaded");
                        Log.Info($"Shell initialization has taken {sw.ElapsedMilliseconds}ms");
                    });
            
            Log.Info($"Loading main window...");
            Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            Application.Current.MainWindow = window;
            
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            splashWindow.Show();
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
            Log.Debug(
                $"Modules list:\n\t{modules.Select(x => new {x.ModuleName, x.ModuleType, x.State, x.InitializationMode, x.DependsOn}).DumpToTable()}");

            var window = (Window)Shell;

            var viewModel = Container.Resolve<IMainWindowViewModel>();
            window.DataContext = viewModel;
        }

        public void Dispose()
        {
            Log.Info("Disposing Chromium...");
            var chromium = Container.Resolve<IChromiumBootstrapper>();
            chromium?.Dispose();
        }
    }
}